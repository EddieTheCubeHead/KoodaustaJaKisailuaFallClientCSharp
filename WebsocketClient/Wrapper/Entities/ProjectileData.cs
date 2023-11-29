namespace WebsocketClient.Wrapper.Entities;

/// <summary>
/// Cell data for projectile type cells
/// </summary>
public record ProjectileData
{
    /// <summary>
    /// The id of the projectile
    /// </summary>
    public string Id { get; init; }
    
    /// <summary>
    /// The coordinates of the projectile
    /// </summary>
    public Coordinates Position { get; init; }
    
    /// <summary>
    /// The direction the projectile is facing to and moving in
    /// </summary>
    public CompassDirection Direction { get; init; }
    
    /// <summary>
    /// The speed of the projectile
    /// </summary>
    public int? Speed { get; init; }
    
    /// <summary>
    /// The mass of the projectile
    /// </summary>
    public int? Mass { get; init; }
};