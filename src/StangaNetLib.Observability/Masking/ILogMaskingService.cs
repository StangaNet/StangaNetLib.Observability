using StangaNetLib.Core.Common;

namespace StangaNetLib.Observability.Masking;

/// <summary>
/// Masks personally identifiable information (PII) before writing it to logs.
/// Implements GDPR Art. 32 (security of processing) by preventing PII from appearing in plain text in log files.
/// Register as singleton via <see cref="StangaNetLib.Observability.Extensions.ServiceCollectionExtensions.AddStangaNetLibObservability"/>.
/// </summary>
public interface ILogMaskingService
{
    /// <summary>
    /// Masks an email address, showing only the first 2 characters of the local part and the full domain.
    /// Example: "john.doe@example.com" → "jo***@example.com"
    /// </summary>
    /// <param name="email">The email address to mask. Null or whitespace returns <c>[empty]</c>.</param>
    /// <returns>A <see cref="Result{T}"/> containing the masked email string.</returns>
    Result<string> MaskEmail(string? email);

    /// <summary>
    /// Returns a short, one-way hash of an email address for log correlation without exposing the address.
    /// Example: "john.doe@example.com" → "a1b2c3d4"
    /// </summary>
    /// <param name="email">The email address to hash. Null or whitespace returns <c>[empty]</c>.</param>
    /// <returns>An 8-character lowercase hex string derived from SHA-256 of the normalised address, wrapped in a <see cref="Result{T}"/>.</returns>
    Result<string> HashEmail(string? email);

    /// <summary>
    /// Masks an IPv4 address by zeroing the last two octets.
    /// For IPv6, shows only the first segment.
    /// Example: "192.168.1.100" → "192.168.0.0"
    /// </summary>
    /// <param name="ipAddress">The IP address to mask. Null or whitespace returns <c>[unknown]</c>.</param>
    /// <returns>The masked IP address string, wrapped in a <see cref="Result{T}"/>.</returns>
    Result<string> MaskIpAddress(string? ipAddress);

    /// <summary>
    /// Masks a phone number, showing only the last 4 digits.
    /// Example: "+1234567890" → "******7890"
    /// </summary>
    /// <param name="phoneNumber">The phone number to mask. Null or whitespace returns <c>[empty]</c>.</param>
    /// <returns>The masked phone number string, wrapped in a <see cref="Result{T}"/>.</returns>
    Result<string> MaskPhoneNumber(string? phoneNumber);

    /// <summary>
    /// Masks a full name, keeping only the first character of each word.
    /// Example: "John Doe" → "J*** D***"
    /// </summary>
    /// <param name="name">The full name to mask. Null or whitespace returns <c>[empty]</c>.</param>
    /// <returns>The masked name string, wrapped in a <see cref="Result{T}"/>.</returns>
    Result<string> MaskName(string? name);

    /// <summary>
    /// Masks a generic string, showing only the first <paramref name="visibleChars"/> characters.
    /// Example: MaskString("SecretToken", 3) → "Sec*****"
    /// </summary>
    /// <param name="value">The value to mask. Null or whitespace returns <c>[empty]</c>.</param>
    /// <param name="visibleChars">Number of leading characters to leave visible. Default: 2.</param>
    /// <returns>The masked string, wrapped in a <see cref="Result{T}"/>.</returns>
    Result<string> MaskString(string? value, int visibleChars = 2);

    /// <summary>
    /// Masks a monetary amount, replacing digits while preserving currency symbols and separators where practical.
    /// Example: <c>"€ 1.234,56"</c> → <c>"€ *.***,**"</c>
    /// </summary>
    Result<string> MaskAmount(string? amount);

    /// <summary>
    /// Masks an IBAN-like value, keeping the first 4 and last 4 characters.
    /// Example: <c>"IT60X0542811101000000123456"</c> → <c>"IT60*******************3456"</c>
    /// </summary>
    Result<string> MaskIban(string? iban);

    /// <summary>
    /// Scrubs free-text descriptions by masking embedded amounts, IBAN-like tokens, emails, and phone numbers.
    /// </summary>
    Result<string> MaskDescription(string? description);

    /// <summary>
    /// Financial / PII preset: applies amount, IBAN, then description scrubbing in sequence.
    /// </summary>
    Result<string> MaskFinancialPii(string? value);
}
