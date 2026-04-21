namespace TicTacToe.Models;

public class GameState
{
    public string GameId { get; init; } = Guid.NewGuid().ToString("N");
    public string?[] Board { get; } = new string?[9];
    public string CurrentTurn { get; set; } = "X";
    public string PlayerX { get; init; } = string.Empty;
    public string PlayerO { get; init; } = string.Empty;
    public bool IsOver { get; set; }
}
