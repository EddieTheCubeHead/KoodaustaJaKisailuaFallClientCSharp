namespace WebsocketClient.Wrapper.Entities;

/// <summary>
/// Persistent context for the tick processing ai function
///
/// You can use this to store data between ticks and add fields as required
/// </summary>
public class TeamAiContext
{
    /// <summary>
    /// The maximum tick length of the game in milliseconds. The wrapper times out tick processing 50 ms before
    /// this time elapses and returns a move 0 command
    /// </summary>
    public int TickLength { get; init; }
    
    /// <summary>
    /// The maximum amount of compass direction steps a ship can turn at a time in the game
    /// </summary>
    public int TurnRate { get; init; }
    
    public TeamAiContext(int tickLength, int turnRate)
    {
        TickLength = tickLength;
        TurnRate = turnRate;
    }
}