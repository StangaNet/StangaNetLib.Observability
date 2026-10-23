using FluentAssertions;
using StangaNetLib.Observability.Masking;

namespace StangaNetLib.Observability.Tests.Masking;

public sealed class FinancialMaskingTests
{
    private readonly LogMaskingService _sut = new();

    [Theory]
    [InlineData("1234.56")]
    [InlineData("€ 1.234,56")]
    [InlineData("USD 10")]
    public void MaskAmount_RedactsDigits(string input)
    {
        var result = _sut.MaskAmount(input);
        result.IsSuccess.Should().BeTrue();
        result.Value!.Any(char.IsDigit).Should().BeFalse();
    }

    [Fact]
    public void MaskIban_KeepsHeadAndTail()
    {
        var iban = "IT60X0542811101000000123456";
        var result = _sut.MaskIban(iban);
        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().StartWith("IT60");
        result.Value!.Should().EndWith("3456");
        result.Value!.Should().Contain("*");
    }

    [Fact]
    public void MaskFinancialPii_ScrubsMixedText()
    {
        var input = "Paid € 99.00 to IT60X0542811101000000123456 for john@example.com";
        var result = _sut.MaskFinancialPii(input);
        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().NotContain("99.00");
        result.Value!.Should().NotContain("IT60X0542811101000000123456");
        result.Value!.Should().Contain("@example.com");
        result.Value!.Should().NotContain("john@example.com");
    }
}
