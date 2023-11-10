namespace WebsocketClient.Wrapper.Entities;

public record Coordinates
{
    public required int X { get; init; }
    public required int Y { get; init; }
};