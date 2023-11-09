namespace WebsocketClient.Entities;

public record ProjectileData
{
    public string Id { get; init; }
    public Coordinates Position { get; init; }
    public CompassDirection Direction { get; init; }
    public int? Velocity { get; init; }
    public int? Mass { get; init; }
    public string Serialize()
    {
        throw new NotImplementedException();
    }
};