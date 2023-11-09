namespace WebsocketClient.Entities;

public record Cell
{
    public required CellType CellType { get; init; }
    public required ProjectileData ProjectileData { get; init; }
    public required ShipData ShipData { get; init; }
    public required HitBoxData HitBoxData { get; init; }
};