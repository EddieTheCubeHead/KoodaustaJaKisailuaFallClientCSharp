namespace WebsocketClient.Entities;

public record GameState
{
    public required int TurnNumber { get; init; }
    public required List<List<Cell>> Cells { get; init; }
};