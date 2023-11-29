namespace WebsocketClient.Wrapper.Entities;

/// <summary>
/// A record representing a command sent from bot to the server
/// </summary>
public record Command
{
    /// <summary>
    /// The type of the action to be performed
    /// </summary>
    public required ActionType Action { get; init; }
    
    /// <summary>
    /// The action data payload specific to the action type to be performed
    /// </summary>
    public required IActionData Payload { get; init; }
}