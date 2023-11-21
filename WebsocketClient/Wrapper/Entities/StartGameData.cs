namespace WebsocketClient.Wrapper.Entities;

public record StartGameData
{
    public required int TickLength { get; init; }
    public required int TurnRate { get; init; }
}