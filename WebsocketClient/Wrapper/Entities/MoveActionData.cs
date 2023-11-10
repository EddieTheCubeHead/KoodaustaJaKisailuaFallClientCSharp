namespace WebsocketClient.Entities;

public record MoveActionData : IActionData
{
    public required int Distance { get; init; }
    public string Serialize()
    {
        throw new NotImplementedException();
    }
}