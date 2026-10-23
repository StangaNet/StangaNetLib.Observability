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

    private static readonly Regex AmountRegex = new(
        @"(?<currency>€|\$|£|USD|EUR|GBP)?\s*(?<amount>\d{1,3}(?:[.,]\d{3})*(?:[.,]\d{1,2})|\d+(?:[.,]\d{1,2})?)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex IbanRegex = new(
        @"\b([A-Z]{2}\d{2}[A-Z0-9]{11,30})\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex InlineEmailRegex = new(
        @"[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex InlinePhoneRegex = new(
        @"\+?\d[\d\s\-().]{6,}\d",
        RegexOptions.Compiled);

    /// <inheritdoc/>
    public Result<string> MaskAmount(string? amount)
    {
        if (string.IsNullOrWhiteSpace(amount)) return Result<string>.Success("[empty]");

        var masked = AmountRegex.Replace(amount, m =>
        {
            var currency = m.Groups["currency"].Success ? m.Groups["currency"].Value + " " : string.Empty;
            var digits = m.Groups["amount"].Value;
            var redacted = new string(digits.Select(c => char.IsDigit(c) ? '*' : c).ToArray());
            return currency + redacted;
        });

        return Result<string>.Success(masked);
    }

    /// <inheritdoc/>
    public Result<string> MaskIban(string? iban)
    {
        if (string.IsNullOrWhiteSpace(iban)) return Result<string>.Success("[empty]");

        var compact = new string(iban.Where(c => !char.IsWhiteSpace(c)).ToArray());
        if (compact.Length < 15)
            return MaskString(compact, 2);

        var visibleHead = Math.Min(4, compact.Length);
        var visibleTail = Math.Min(4, compact.Length - visibleHead);
        var middle = Math.Max(0, compact.Length - visibleHead - visibleTail);
        return Result<string>.Success(compact[..visibleHead] + new string('*', middle) + compact[^visibleTail..]);
    }

    /// <inheritdoc/>
    public Result<string> MaskDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description)) return Result<string>.Success("[empty]");

        var text = IbanRegex.Replace(description, m => MaskIban(m.Value).Value!);
        text = AmountRegex.Replace(text, m => MaskAmount(m.Value).Value!);
        text = InlineEmailRegex.Replace(text, m => MaskEmail(m.Value).Value!);
        text = InlinePhoneRegex.Replace(text, m => MaskPhoneNumber(m.Value).Value!);
        return Result<string>.Success(text);
    }

    /// <inheritdoc/>
    public Result<string> MaskFinancialPii(string? value)
        // Description scrub already redacts amounts, IBAN-like tokens, emails, and phones.
        => MaskDescription(value);
}
