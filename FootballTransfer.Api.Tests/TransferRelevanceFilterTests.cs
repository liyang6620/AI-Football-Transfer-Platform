using FootballTransfer.Api.Services;

namespace FootballTransfer.Api.Tests;

public class TransferRelevanceFilterTests
{
    [Theory]
    [InlineData("Player signs for new club", "The move was announced today.")]
    [InlineData("Club update", "The player has agreed a deal to move to the club.")]
    public void IsTransferRelated_AcceptsExistingPositiveSignals(string title, string content)
    {
        Assert.True(TransferRelevanceFilter.IsTransferRelated(title, content));
    }

    [Theory]
    [InlineData("Summer done deals", "All transfers completed this window.")]
    [InlineData("World Cup group preview", "A player profile before the match.")]
    public void IsTransferRelated_RejectsExistingNegativeSignals(string title, string content)
    {
        Assert.False(TransferRelevanceFilter.IsTransferRelated(title, content));
    }
}
