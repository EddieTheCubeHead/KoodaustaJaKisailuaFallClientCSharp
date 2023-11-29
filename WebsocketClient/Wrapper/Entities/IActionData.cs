namespace WebsocketClient.Wrapper.Entities;

/// <summary>
/// Shared interface for all bot actions
/// </summary>
public interface IActionData
{
    /// <summary>
    /// Serialize the action data to a json string
    /// </summary>
    /// <returns>The action data serialized as json in a string format</returns>
    string Serialize();
};