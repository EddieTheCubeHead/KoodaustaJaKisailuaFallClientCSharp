namespace WebsocketClient.Wrapper.Entities;

/// <summary>
/// Action data for the shoot action
/// </summary>
public record ShootActionData : IActionData
{
    /// <summary>
    /// The mass of the projectile to shoot
    /// </summary>
    public required int Mass { get; init; }
    
    /// <summary>
    /// The speed of the projectile to shoot
    /// </summary>
    public required int Speed { get; init; }
    
    /// <summary>
    /// Serialize the action data to a json string
    /// </summary>
    /// <returns>The action data serialized as json in a string format</returns>
    public string Serialize()
    {
        return $"{{\"speed\": {Speed},\"mass\": {Mass}}}";
    }
}