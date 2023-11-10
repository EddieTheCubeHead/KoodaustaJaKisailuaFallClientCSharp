namespace WebsocketClient.Entities;

public record Cell
{
    public required CellType CellType { get; init; }
    public ProjectileData? ProjectileData { get; init; }
    public ShipData? ShipData { get; init; }
    public HitBoxData? HitBoxData { get; init; }
};