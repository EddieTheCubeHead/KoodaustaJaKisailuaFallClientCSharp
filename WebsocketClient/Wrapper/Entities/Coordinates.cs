namespace WebsocketClient.Wrapper.Entities;

/// <summary>
/// A record containing the coordinates of a point on the map, or a vector
/// </summary>
public record Coordinates
{
    /// <summary>
    /// The x value of the coordinates or the vector
    /// </summary>
    public required int X { get; init; }
    
    /// <summary>
    /// The y value of the coordinates or the vector
    /// </summary>
    public required int Y { get; init; }
};