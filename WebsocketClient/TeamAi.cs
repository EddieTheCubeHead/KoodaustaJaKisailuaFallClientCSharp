using Microsoft.Extensions.Logging;
using WebsocketClient.Entities;
using WebsocketClient.Wrapper.Entities;

namespace WebsocketClient;

public class TeamAi
{
    public TeamAiContext Context = null;
    private readonly ILogger _logger;
    
    public TeamAi(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<TeamAi>();
    }

    public void CreateContext(StartGameData gameData)
    {
        Context = new TeamAiContext(gameData.TickLength, gameData.TurnRate);
    }

    public Command? ProcessTick(GameState gameState)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogDebug("Processing tick.");
        
        // Your code goes here
        
        timer.Stop();
        _logger.LogInformation($"tick processed in {timer.ElapsedMilliseconds} milliseconds");
        return null;
    }
    
}