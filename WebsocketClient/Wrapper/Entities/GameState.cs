using WebsocketClient.Entities;

namespace WebsocketClient.Wrapper.Entities;

public record GameState
{
    public required int TurnNumber { get; init; }
    public required List<List<Cell>> GameMap { get; init; }
};