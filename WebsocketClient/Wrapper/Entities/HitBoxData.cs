namespace WebsocketClient.Wrapper.Entities;


/// <summary>
/// Cell data for hit box type cells
/// </summary>
public record HitBoxData
{
    /// <summary>
    /// The id of the entity that owns the hit box
    /// </summary>
    public required string EntityId { get; init; }
};