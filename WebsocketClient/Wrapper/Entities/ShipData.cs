namespace WebsocketClient.Wrapper.Entities;

/// <summary>
/// Cell data for ship type cells
/// </summary>
public record ShipData
{
    /// <summary>
    /// The id of the ship
    /// </summary>
    public string Id { get; init; }
    
    /// <summary>
    /// The coordinates of the ship
    /// </summary>
    public Coordinates Position { get; init; }
    
    /// <summary>
    /// The direction the ship is facing towards
    /// </summary>
    public CompassDirection Direction { get; init; }
    
    /// <summary>
    /// The remaining health of the ship. Max value 25
    /// </summary>
    public int? Health { get; init; }
    
    /// <summary>
    /// The accrued heat of the ship. Max value 25, accumulating heat while at 25 heat will cause damage
    /// in a ratio of 1 heat to 1 damage to the ship
    /// </summary>
    public int? Heat { get; init; }
};