using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using StangaNetLib.Core.Common;

namespace StangaNetLib.Observability.Masking;

/// <summary>
/// Default implementation of <see cref="ILogMaskingService"/>.
/// </summary>
public sealed class LogMaskingService : ILogMaskingService
{
    private static readonly Regex EmailRegex =
        new(@"^([^@]+)@(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex IpV4Regex =
        new(@"^(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})$", RegexOptions.Compiled);

    /// <inheritdoc/>
    public Result<string> MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return Result<string>.Success("[empty]");

        var match = EmailRegex.Match(email);
        if (!match.Success) return MaskString(email, 2);

        var local = match.Groups[1].Value;
        var domain = match.Groups[2].Value;
        var maskedLocal = local.Length <= 2
            ? local
            : local[..2] + new string('*', Math.Min(local.Length - 2, 3));

        return Result<string>.Success($"{maskedLocal}@{domain}");
    }

    /// <inheritdoc/>
    public Result<string> HashEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return Result<string>.Success("[empty]");

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(email.ToLowerInvariant().Trim()));
        return Result<string>.Success(Convert.ToHexString(bytes)[..8].ToLowerInvariant());
    }

    /// <inheritdoc/>
    public Result<string> MaskIpAddress(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress)) return Result<string>.Success("[unknown]");

        // IPv6 — show first segment only
        if (ipAddress.Contains(':'))
        {
            var firstColon = ipAddress.IndexOf(':');
            return Result<string>.Success(ipAddress[..firstColon] + ":****");
        }

        // IPv4 — zero last two octets
        var match = IpV4Regex.Match(ipAddress);
        return Result<string>.Success(match.Success
            ? $"{match.Groups[1].Value}.{match.Groups[2].Value}.0.0"
            : "[masked]");
    }

    /// <inheritdoc/>
    public Result<string> MaskPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber)) return Result<string>.Success("[empty]");

        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
        if (digits.Length <= 4) return Result<string>.Success(new string('*', digits.Length));
        return Result<string>.Success(new string('*', digits.Length - 4) + digits[^4..]);
    }

    /// <inheritdoc/>
    public Result<string> MaskName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return Result<string>.Success("[empty]");

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var maskedParts = parts.Select(p =>
        {
            if (p.Length == 0) return string.Empty;
            return p[0] + new string('*', Math.Min(p.Length - 1, 3));
        });
        return Result<string>.Success(string.Join(" ", maskedParts));
    }

    /// <inheritdoc/>
    public Result<string> MaskString(string? value, int visibleChars = 2)
    {
        if (string.IsNullOrWhiteSpace(value)) return Result<string>.Success("[empty]");
        if (value.Length <= visibleChars) return Result<string>.Success(value);
        return Result<string>.Success(value[..visibleChars] + new string('*', Math.Min(value.Length - visibleChars, 5)));
    }
}
