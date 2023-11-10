using WebsocketClient.Entities;
using System;

namespace WebsocketClient.Wrapper.Entities;

public record TurnActionData : IActionData
{
    public required CompassDirection Direction { get; init; }
    public string Serialize()
    {
        var lowercaseDirection = string.Concat(Direction.ToString()[..1].ToLower(),
            Direction.ToString().AsSpan(1));
        return
            $"{{\"direction\":\"{lowercaseDirection}\"}}";
    }
}