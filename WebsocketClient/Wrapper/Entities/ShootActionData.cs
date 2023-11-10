namespace WebsocketClient.Entities;

public record ShootActionData : IActionData
{
    public required int Mass { get; init; }
    public required int Speed { get; init; }
    public string Serialize()
    {
        throw new NotImplementedException();
    }
}