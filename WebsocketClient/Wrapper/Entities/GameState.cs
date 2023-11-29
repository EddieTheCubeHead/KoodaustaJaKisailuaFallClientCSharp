namespace WebsocketClient.Wrapper.Entities;

/// <summary>
/// The state of the game at the beginning of a tick
/// </summary>
public record GameState
{
    /// <summary>
    /// The current turn number
    /// </summary>
    public required int TurnNumber { get; init; }
    
    /// <summary>
    /// A list of lists (matrix) of cells representing the game map
    /// </summary>
    public required List<List<Cell>> GameMap { get; init; }
};