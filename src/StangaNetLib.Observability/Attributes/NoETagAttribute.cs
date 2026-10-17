namespace StangaNetLib.Observability.Attributes;

/// <summary>
/// Prevents the <see cref="StangaNetLib.Observability.Middleware.ETagMiddleware"/> from processing this endpoint.
/// Use on real-time or always-changing endpoints where ETags provide no benefit.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class NoETagAttribute : Attribute { }
