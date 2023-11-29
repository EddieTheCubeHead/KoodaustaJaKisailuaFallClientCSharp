namespace WebsocketClient.Wrapper.Entities;

/// <summary>
/// Action data specific to the move action
/// </summary>
public record MoveActionData : IActionData
{
    /// <summary>
    /// The distance to move, should be between 0 and 3. Validated server-side
    /// </summary>
    public required int Distance { get; init; }
    
    /// <summary>
    /// Serialize the move action data to a json string
    /// </summary>
    /// <returns>The action data serialized as json in a string format</returns>
    public string Serialize()
    {
        return $"{{\"distance\":{Distance}}}";
    }
}