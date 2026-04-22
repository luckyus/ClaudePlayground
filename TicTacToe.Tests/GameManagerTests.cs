using TicTacToe.Services;

namespace TicTacToe.Tests;

public class GameManagerTests
{
    [Fact]
    public void TryMatchPlayer_TwoPlayers_ReturnsPairedGame()
    {
        var manager = new GameManager();

        var result1 = manager.TryMatchPlayer("player1");
        var result2 = manager.TryMatchPlayer("player2");

        Assert.Null(result1);        // first player waits
        Assert.NotNull(result2);     // second player gets a game
        Assert.Equal("player1", result2.PlayerX);
        Assert.Equal("player2", result2.PlayerO);
    }

    [Fact]
    public void MakeMove_ValidMove_PlacesSymbol()
    {
        var manager = new GameManager();
        manager.TryMatchPlayer("player1");
        var game = manager.TryMatchPlayer("player2")!;

        var (result, _) = manager.MakeMove("player1", 0); // X goes first

        Assert.NotNull(result);
        Assert.Equal("X", result.Board[0]);
    }

    [Fact]
    public void MakeMove_WinningMove_ReturnsWinner()
    {
        var manager = new GameManager();
        manager.TryMatchPlayer("px");
        manager.TryMatchPlayer("po");

        // X: 0, 1, 2  (top row) — O plays 3, 4 between X's moves
        manager.MakeMove("px", 0);
        manager.MakeMove("po", 3);
        manager.MakeMove("px", 1);
        manager.MakeMove("po", 4);
        var (result, _) = manager.MakeMove("px", 2);

        Assert.NotNull(result);
        Assert.Equal("X", result.Winner);
        Assert.Equal([0, 1, 2], result.WinLine);
    }

    [Fact]
    public void MakeMove_OccupiedCell_ReturnsNull()
    {
        var manager = new GameManager();
        manager.TryMatchPlayer("px");
        manager.TryMatchPlayer("po");

        manager.MakeMove("px", 4);
        var (result, _) = manager.MakeMove("po", 4); // same cell

        Assert.Null(result);
    }
}
