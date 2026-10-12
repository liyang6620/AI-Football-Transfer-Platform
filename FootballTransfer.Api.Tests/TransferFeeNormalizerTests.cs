using FootballTransfer.Api.Services;

namespace FootballTransfer.Api.Tests;

public sealed class TransferFeeNormalizerTests
{
    [Theory]
    [InlineData("Club sign player for GBP 500,000", "500", "0.5")]
    [InlineData("Rangers agree GBP 800 000 option", "800", "0.8")]
    [InlineData("Deal worth EUR 300k", "300", "0.3")]
    [InlineData("A GBP 86m transfer", "86", "86")]
    [InlineData("A USD 1.2bn package", "1200", "1200")]
    public void NormalizeToMillions_uses_the_explicit_source_unit(string text, string modelValue, string expected)
    {
        Assert.Equal(
            decimal.Parse(expected),
            TransferFeeNormalizer.NormalizeToMillions(decimal.Parse(modelValue), text));
    }

    [Fact]
    public void NormalizeToMillions_keeps_model_value_when_source_has_no_clear_amount()
    {
        Assert.Equal(42, TransferFeeNormalizer.NormalizeToMillions(42, "Fee was not disclosed."));
    }

    [Fact]
    public void NormalizeToMillions_does_not_replace_the_model_value_with_an_unrelated_amount()
    {
        const string text = "The club has a GBP 90m budget and completed this transfer for GBP 25m.";

        Assert.Equal(25, TransferFeeNormalizer.NormalizeToMillions(25, text));
    }
}
