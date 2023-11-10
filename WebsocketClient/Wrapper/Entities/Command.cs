using WebsocketClient.Wrapper.Entities;

namespace WebsocketClient.Entities;

public record Command
{
    public required ActionType Action { get; init; }
    public required IActionData ActionData { get; init; }
}