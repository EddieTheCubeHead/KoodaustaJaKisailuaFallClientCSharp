namespace WebsocketClient.Entities;

public record TurnActionData : IActionData
{
    public required CompassDirection Direction { get; init; }
    public string Serialize()
    {
        throw new NotImplementedException();
    }
}