using FootballTransfer.Api.Services;

namespace FootballTransfer.Api.Tests;

public sealed class ClubNameNormalizerTests
{
    [Theory]
    [InlineData("Chelsea Football Club", "Chelsea")]
    [InlineData("PSG", "Paris Saint-Germain")]
    [InlineData("Real Madrid CF", "Real Madrid")]
    [InlineData("  Arsenal   FC  ", "Arsenal")]
    [InlineData("Man Utd", "Manchester United")]
    [InlineData("Spurs", "Tottenham Hotspur")]
    public void Normalize_returns_stable_club_names(string input, string expected)
    {
        Assert.Equal(expected, ClubNameNormalizer.Normalize(input));
    }

    [Fact]
    public void Key_matches_known_aliases()
    {
        Assert.Equal(ClubNameNormalizer.Key("PSG"), ClubNameNormalizer.Key("Paris Saint-Germain FC"));
    }
}
