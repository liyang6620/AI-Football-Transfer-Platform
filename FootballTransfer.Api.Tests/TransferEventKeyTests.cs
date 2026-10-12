using FootballTransfer.Api.Services;

namespace FootballTransfer.Api.Tests;

public sealed class TransferEventKeyTests
{
    [Fact]
    public void Equivalent_names_and_movement_types_share_an_event_key()
    {
        var rumour = TransferEventKey.Create("Joao Pedro", "Manchester United FC", "Rumour");
        var completed = TransferEventKey.Create("Joao Pedro", "Man Utd", "Completed Transfer");

        Assert.Equal(rumour, completed);
    }

    [Fact]
    public void Contract_is_separate_from_a_club_move()
    {
        var contract = TransferEventKey.Create("Joao Pedro", "Manchester United", "Contract");
        var movement = TransferEventKey.Create("Joao Pedro", "Manchester United", "Rumour");

        Assert.NotEqual(contract, movement);
    }
}
