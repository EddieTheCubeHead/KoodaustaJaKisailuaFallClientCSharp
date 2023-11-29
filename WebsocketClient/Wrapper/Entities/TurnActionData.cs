namespace WebsocketClient.Wrapper.Entities;

/// <summary>
/// Action data for the turn action
/// </summary>
public record TurnActionData : IActionData
{
    /// <summary>
    /// The direction the ship should be facing after turning. Validated to be at maximum the game's max turn rate
    /// compass direction steps away from the ship starting position
    /// </summary>
    public required CompassDirection Direction { get; init; }
    
    /// <summary>
    /// Serialize the action data to a json string
    /// </summary>
    /// <returns>The action data serialized as json in a string format</returns>
    public string Serialize()
    {
        var lowercaseDirection = string.Concat(Direction.ToString()[..1].ToLower(),
            Direction.ToString().AsSpan(1));
        return
            $"{{\"direction\":\"{lowercaseDirection}\"}}";
    }
}