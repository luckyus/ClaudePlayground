using Microsoft.AspNetCore.SignalR;
using TicTacToe.Services;

namespace TicTacToe.Hubs;

public class GameHub(GameManager gameManager) : Hub
{
    public async Task JoinQueue()
    {
        var game = gameManager.TryMatchPlayer(Context.ConnectionId);

        if (game is null)
        {
            await Clients.Caller.SendAsync("Waiting");
            return;
        }

        await Groups.AddToGroupAsync(game.PlayerX, game.GameId);
        await Groups.AddToGroupAsync(game.PlayerO, game.GameId);

        await Clients.Client(game.PlayerX).SendAsync("GameStarted", "X");
        await Clients.Client(game.PlayerO).SendAsync("GameStarted", "O");
    }

    public async Task MakeMove(int cellIndex)
    {
        var (result, game) = gameManager.MakeMove(Context.ConnectionId, cellIndex);
        if (result is null || game is null) return;

        if (result.Winner is not null || result.IsDraw)
        {
            await Clients.Group(game.GameId).SendAsync("GameOver", result.Board, result.Winner, result.WinLine);
        }
        else
        {
            await Clients.Group(game.GameId).SendAsync("BoardUpdated", result.Board, result.CurrentTurn);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var (_, opponentId) = gameManager.RemovePlayer(Context.ConnectionId);

        if (opponentId is not null)
        {
            await Clients.Client(opponentId).SendAsync("OpponentDisconnected");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
