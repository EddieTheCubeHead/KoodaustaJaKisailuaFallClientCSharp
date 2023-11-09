namespace WebsocketClient.Entities;

public record ShipData
{
    public string Id { get; init; }
    public Coordinates Position { get; init; }
    public CompassDirection Direction { get; init; }
    public int? Health { get; init; }
    public int? Heat { get; init; }
    public string Serialize()
    {
        throw new NotImplementedException();
    }
};