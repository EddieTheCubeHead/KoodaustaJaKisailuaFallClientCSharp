namespace WebsocketClient.Entities;

public record HitBoxData
{
    public required string EntityId { get; init; }
    public string Serialize()
    {
        throw new NotImplementedException();
    }
};