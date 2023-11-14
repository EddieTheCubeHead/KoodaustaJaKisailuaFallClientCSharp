using WebsocketClient.Entities;
using WebsocketClient.Wrapper.Entities;

namespace WebsocketClient;

public class TeamAi
{
    private TeamAiContext _context = new();

    public void ResetContext()
    {
        _context = new TeamAiContext();
    }

    public Command? ProcessTick(GameState gameState)
    {
        // Your code goes here
        return null;
    }
    
}