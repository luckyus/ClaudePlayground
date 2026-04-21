using System.Collections.Concurrent;
using TicTacToe.Models;

namespace TicTacToe.Services;

public record MoveResult(string?[] Board, string CurrentTurn, string? Winner, int[]? WinLine, bool IsDraw);

public class GameManager
{
    private static readonly int[][] WinLines =
    [
        [0, 1, 2], [3, 4, 5], [6, 7, 8],
        [0, 3, 6], [1, 4, 7], [2, 5, 8],
        [0, 4, 8], [2, 4, 6]
    ];

    private readonly ConcurrentQueue<string> _waitingPlayers = new();
    private readonly ConcurrentDictionary<string, GameState> _games = new();
    private readonly ConcurrentDictionary<string, string> _playerToGame = new();

    // Returns the paired GameState if matched, null if now waiting.
    public GameState? TryMatchPlayer(string connectionId)
    {
        if (_waitingPlayers.TryDequeue(out var waitingId))
        {
            var game = new GameState { PlayerX = waitingId, PlayerO = connectionId };
            _games[game.GameId] = game;
            _playerToGame[waitingId] = game.GameId;
            _playerToGame[connectionId] = game.GameId;
            return game;
        }

        _waitingPlayers.Enqueue(connectionId);
        return null;
    }

    public (MoveResult? result, GameState? game) MakeMove(string connectionId, int cellIndex)
    {
        if (!_playerToGame.TryGetValue(connectionId, out var gameId)) return (null, null);
        if (!_games.TryGetValue(gameId, out var game)) return (null, null);

        var symbol = game.PlayerX == connectionId ? "X" : "O";

        lock (game)
        {
            if (game.IsOver || game.Board[cellIndex] is not null || game.CurrentTurn != symbol)
                return (null, null);

            game.Board[cellIndex] = symbol;

            var winLine = GetWinLine(game.Board, symbol);
            if (winLine is not null)
            {
                game.IsOver = true;
                return (new MoveResult(game.Board, game.CurrentTurn, symbol, winLine, false), game);
            }

            if (game.Board.All(c => c is not null))
            {
                game.IsOver = true;
                return (new MoveResult(game.Board, game.CurrentTurn, null, [], true), game);
            }

            game.CurrentTurn = symbol == "X" ? "O" : "X";
            return (new MoveResult(game.Board, game.CurrentTurn, null, null, false), game);
        }
    }

    // Returns the gameId of the game the player was in (if any), plus the opponent's connection ID.
    public (string? gameId, string? opponentId) RemovePlayer(string connectionId)
    {
        _playerToGame.TryRemove(connectionId, out var gameId);

        // Remove from waiting queue by draining and re-enqueuing everyone else.
        var remaining = new List<string>();
        while (_waitingPlayers.TryDequeue(out var id))
        {
            if (id != connectionId) remaining.Add(id);
        }
        foreach (var id in remaining) _waitingPlayers.Enqueue(id);

        if (gameId is null) return (null, null);

        _games.TryGetValue(gameId, out var game);
        if (game is null) return (gameId, null);

        game.IsOver = true;
        var opponentId = game.PlayerX == connectionId ? game.PlayerO : game.PlayerX;

        _games.TryRemove(gameId, out _);
        _playerToGame.TryRemove(opponentId, out _);

        return (gameId, opponentId);
    }

    public string? GetSymbol(string connectionId, string gameId)
    {
        if (!_games.TryGetValue(gameId, out var game)) return null;
        return game.PlayerX == connectionId ? "X" : "O";
    }

    private static int[]? GetWinLine(string?[] board, string symbol)
    {
        foreach (var line in WinLines)
        {
            if (line.All(i => board[i] == symbol)) return line;
        }
        return null;
    }
}
