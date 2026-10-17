using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using StangaNetLib.Core.Exceptions;
using StangaNetLib.Core.Guards;
using StangaNetLib.Observability.Correlation;
using StangaNetLib.Observability.Options;

namespace StangaNetLib.Observability.Middleware;

/// <summary>
/// Catches unhandled exceptions, logs them, and returns a structured ProblemDetails (RFC 7807) response.
/// <see cref="OperationCanceledException"/> caused by client disconnection is logged at Information level
/// and produces no response. Exception details are included in Development or when
/// <see cref="ObservabilityOptions.IncludeExceptionDetailsInResponse"/> is true.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly ObservabilityOptions _options;
    private readonly bool _isDevelopment;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Initializes the middleware with the pipeline delegate, logger, options, and hosting environment.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">Logger used to record unhandled exceptions.</param>
    /// <param name="options">Observability configuration.</param>
    /// <param name="env">Hosting environment; enables exception details automatically in Development.</param>
    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IOptions<ObservabilityOptions> options,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
        _isDevelopment = env.IsDevelopment();
    }

    /// <summary>
    /// Invokes the next middleware and catches any unhandled exception, writing a structured
    /// ProblemDetails JSON response. Client-abort exceptions are swallowed after an Information log.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="correlationIdAccessor">Provides the correlation ID included in error responses.</param>
    public async Task InvokeAsync(HttpContext context, ICorrelationIdAccessor correlationIdAccessor)
    {
        Guard.Against.Null(context, nameof(context));
        Guard.Against.Null(correlationIdAccessor, nameof(correlationIdAccessor));

        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            var cid = correlationIdAccessor.CorrelationId ?? "-";
            _logger.LogInformation(
                "Request cancelled by client [{CorrelationId}] {Method} {Path}",
                cid, context.Request.Method, context.Request.Path);
        }
        catch (Exception ex)
        {
            var cid = correlationIdAccessor.CorrelationId ?? Guid.NewGuid().ToString("N")[..8];

            _logger.LogError(ex,
                "Unhandled exception [{CorrelationId}] {Method} {Path}",
                cid, context.Request.Method, context.Request.Path);

            if (!context.Response.HasStarted)
                await WriteErrorResponseAsync(context, ex, cid);
        }
    }

    private async Task WriteErrorResponseAsync(HttpContext context, Exception ex, string correlationId)
    {
        var statusCode = MapToStatusCode(ex);
        var includeDetails = _isDevelopment || _options.IncludeExceptionDetailsInResponse;

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = GetPublicTitle(ex),
            Detail = GetPublicMessage(ex, includeDetails)
        };

        problem.Extensions["correlationId"] = correlationId;

        if (includeDetails)
        {
            problem.Extensions["exceptionType"] = ex.GetType().Name;
            problem.Extensions["stackTrace"] = ex.StackTrace;
            if (ex.InnerException is not null)
                problem.Extensions["innerException"] = ex.InnerException.Message;
        }
        else
        {
            problem.Extensions["support"] = $"Reference this ID if you contact support: {correlationId}";
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }

    private int MapToStatusCode(Exception ex)
    {
        if (_options.CustomExceptionStatusCodes.TryGetValue(ex.GetType(), out var custom))
            return custom;

        return ex switch
        {
            ArgumentNullException => (int)HttpStatusCode.BadRequest,
            ArgumentException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            NotSupportedException => (int)HttpStatusCode.BadRequest,
            DomainException => (int)HttpStatusCode.BadRequest,
            _ => (int)HttpStatusCode.InternalServerError
        };
    }

    private static string GetPublicTitle(Exception ex) => ex switch
    {
        ArgumentException => "Invalid Request",
        UnauthorizedAccessException => "Unauthorized",
        KeyNotFoundException => "Not Found",
        DomainException => "Domain Error",
        _ => "Internal Server Error"
    };

    private static string GetPublicMessage(Exception ex, bool includeDetails) => ex switch
    {
        ArgumentException when includeDetails => ex.Message,
        ArgumentException => "The request contained invalid data.",
        UnauthorizedAccessException => "You are not authorized to perform this action.",
        KeyNotFoundException => "The requested resource was not found.",
        DomainException => ex.Message,
        _ when includeDetails => ex.Message,
        _ => "An internal error occurred. Please try again later."
    };
}
