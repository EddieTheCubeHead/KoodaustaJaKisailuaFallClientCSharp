namespace WebsocketClient.Wrapper.Entities;

/// <summary>
/// A record containing the data for one map cell
/// </summary>
public record Cell
{
    /// <summary>
    /// The type of the cell
    /// </summary>
    public required CellType CellType { get; init; }
    
    /// <summary>
    /// The projectile data in the cell, if cell has a projectile
    /// </summary>
    public ProjectileData? ProjectileData { get; init; }
    
    /// <summary>
    /// The ship data in the cell, if cell has a ship
    /// </summary>
    public ShipData? ShipData { get; init; }
    
    /// <summary>
    /// the hit box data in the cell, if cell has a hit box
    /// </summary>
    public HitBoxData? HitBoxData { get; init; }
};