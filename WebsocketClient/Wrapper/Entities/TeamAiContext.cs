namespace WebsocketClient.Wrapper.Entities;

public class TeamAiContext
{
    public int TickLength { get; init; }
    public int TurnRate { get; init; }
    
    public TeamAiContext(int tickLength, int turnRate)
    {
        TickLength = tickLength;
        TurnRate = turnRate;
    }
}