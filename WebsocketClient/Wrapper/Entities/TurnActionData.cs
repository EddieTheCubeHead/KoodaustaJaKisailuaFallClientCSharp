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


    private static Dictionary<CompassDirection, string> _directionMappings = new()
    {
        { CompassDirection.North, "n"},
        {  CompassDirection.NorthEast, "ne"},
        { CompassDirection.East, "e"},
        {  CompassDirection.SouthEast, "se"},
        { CompassDirection.South, "s"},
        {  CompassDirection.SouthWest, "sw"},
        { CompassDirection.West, "w"},
        {  CompassDirection.NorthWest, "nw"}
    };
    /// <summary>
    /// Serialize the action data to a json string
    /// </summary>
    /// <returns>The action data serialized as json in a string format</returns>
    public string Serialize()
    {

        var lowercaseDirection = _directionMappings[Direction];
        return
            $"{{\"direction\":\"{lowercaseDirection}\"}}";
    }
}