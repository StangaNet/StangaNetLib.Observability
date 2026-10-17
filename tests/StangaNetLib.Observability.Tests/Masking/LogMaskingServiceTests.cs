using FluentAssertions;
using StangaNetLib.Observability.Masking;

namespace StangaNetLib.Observability.Tests.Masking;

public sealed class LogMaskingServiceTests
{
    private readonly LogMaskingService _sut = new();

    // ── MaskEmail ─────────────────────────────────────────────────────────

    [Fact]
    public void MaskEmail_StandardAddress_ShowsFirstTwoCharsAndFullDomain()
    {
        var result = _sut.MaskEmail("john.doe@example.com");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().StartWith("jo");
        result.Value.Should().EndWith("@example.com");
        result.Value.Should().Contain("*");
    }

    [Fact]
    public void MaskEmail_ShortLocalPart_DoesNotMaskBelow2Chars()
    {
        var result = _sut.MaskEmail("ab@example.com");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().StartWith("ab@");
    }

    [Fact]
    public void MaskEmail_NullOrEmpty_ReturnsEmptyPlaceholder()
    {
        _sut.MaskEmail(null).Value.Should().Be("[empty]");
        _sut.MaskEmail("").Value.Should().Be("[empty]");
        _sut.MaskEmail("   ").Value.Should().Be("[empty]");
    }

    // ── HashEmail ─────────────────────────────────────────────────────────

    [Fact]
    public void HashEmail_Returns8CharLowercaseHex()
    {
        var result = _sut.HashEmail("user@example.com");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().MatchRegex("^[0-9a-f]{8}$");
    }

    [Fact]
    public void HashEmail_SameEmailNormalized_ProducesConsistentHash()
    {
        var h1 = _sut.HashEmail("User@Example.COM");
        var h2 = _sut.HashEmail("user@example.com");

        h1.Value.Should().Be(h2.Value, "hash is computed on lowercased, trimmed address");
    }

    [Fact]
    public void HashEmail_DifferentAddresses_ProduceDifferentHashes()
    {
        _sut.HashEmail("alice@example.com").Value.Should().NotBe(_sut.HashEmail("bob@example.com").Value);
    }

    [Fact]
    public void HashEmail_NullOrEmpty_ReturnsEmptyPlaceholder()
    {
        _sut.HashEmail(null).Value.Should().Be("[empty]");
        _sut.HashEmail("").Value.Should().Be("[empty]");
    }

    // ── MaskIpAddress ─────────────────────────────────────────────────────

    [Fact]
    public void MaskIpAddress_IPv4_ZerosLastTwoOctets()
    {
        _sut.MaskIpAddress("192.168.1.100").Value.Should().Be("192.168.0.0");
        _sut.MaskIpAddress("10.0.0.1").Value.Should().Be("10.0.0.0");
    }

    [Fact]
    public void MaskIpAddress_IPv6_ShowsOnlyFirstSegment()
    {
        var result = _sut.MaskIpAddress("2001:0db8:85a3:0000:0000:8a2e:0370:7334");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().StartWith("2001");
        result.Value.Should().Contain(":****");
        result.Value.Should().NotContain("0db8");
    }

    [Fact]
    public void MaskIpAddress_NullOrEmpty_ReturnsUnknownPlaceholder()
    {
        _sut.MaskIpAddress(null).Value.Should().Be("[unknown]");
        _sut.MaskIpAddress("").Value.Should().Be("[unknown]");
    }

    // ── MaskPhoneNumber ───────────────────────────────────────────────────

    [Fact]
    public void MaskPhoneNumber_ShowsOnlyLast4Digits()
    {
        var result = _sut.MaskPhoneNumber("+1234567890");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().EndWith("7890");
        result.Value.Should().NotContain("1234");
    }

    [Fact]
    public void MaskPhoneNumber_ShortNumber_MasksAllDigits()
    {
        var result = _sut.MaskPhoneNumber("1234");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("****");
    }

    [Fact]
    public void MaskPhoneNumber_NullOrEmpty_ReturnsEmptyPlaceholder()
    {
        _sut.MaskPhoneNumber(null).Value.Should().Be("[empty]");
        _sut.MaskPhoneNumber("").Value.Should().Be("[empty]");
    }

    // ── MaskName ──────────────────────────────────────────────────────────

    [Fact]
    public void MaskName_FullName_ShowsFirstCharOfEachWord()
    {
        var result = _sut.MaskName("John Doe");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().StartWith("J");
        result.Value.Should().Contain("D");
        result.Value.Should().Contain("*");
    }

    [Fact]
    public void MaskName_SingleName_ShowsFirstChar()
    {
        var result = _sut.MaskName("Alice");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().StartWith("A");
        result.Value.Should().Contain("*");
    }

    [Fact]
    public void MaskName_NullOrEmpty_ReturnsEmptyPlaceholder()
    {
        _sut.MaskName(null).Value.Should().Be("[empty]");
        _sut.MaskName("").Value.Should().Be("[empty]");
    }

    // ── MaskString ────────────────────────────────────────────────────────

    [Fact]
    public void MaskString_Default_ShowsFirstTwoChars()
    {
        var result = _sut.MaskString("SecretToken123");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().StartWith("Se");
        result.Value.Should().Contain("*");
    }

    [Fact]
    public void MaskString_CustomVisibleChars_RespectsCount()
    {
        var result = _sut.MaskString("AbcDefGhi", visibleChars: 4);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().StartWith("AbcD");
        result.Value.Should().Contain("*");
    }

    [Fact]
    public void MaskString_ShortValue_ReturnsAsIs()
    {
        _sut.MaskString("AB").Value.Should().Be("AB");
        _sut.MaskString("A").Value.Should().Be("A");
    }

    [Fact]
    public void MaskString_NullOrEmpty_ReturnsEmptyPlaceholder()
    {
        _sut.MaskString(null).Value.Should().Be("[empty]");
        _sut.MaskString("").Value.Should().Be("[empty]");
    }
}
