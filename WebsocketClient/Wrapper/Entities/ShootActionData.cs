using WebsocketClient.Entities;

namespace WebsocketClient.Wrapper.Entities;

public record ShootActionData : IActionData
{
    public required int Mass { get; init; }
    public required int Speed { get; init; }
    public string Serialize()
    {
        return $"{{\"speed\": {Speed},\"mass\": {Mass}}}";
    }
}