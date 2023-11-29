using Microsoft.Extensions.Logging;
using WebsocketClient.Wrapper.Entities;

namespace WebsocketClient;

/// <summary>
/// The class that is responsible for handling a team's AI logic for each tick
/// </summary>
public class TeamAi
{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
    /// <summary>
    /// The persistent context maintained between ticks
    /// </summary>
    public TeamAiContext Context = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    
    /// <summary>
    /// You can use this logger to track the behaviour of your bot. 
    ///
    /// This is preferred to calling print("msg") as it offers better configuration (see README.md in root)
    /// </summary>
    ///
    /// <example>
    /// _logger.LogDebug("A message that is not important but helps understand the code during problem solving.")
    /// _logger.LogInfo("A message that you want to see to know the state of the bot during normal operation.")
    /// _logger.LogWarning("A message that demands attention, but is not yet causing problems.")
    /// _logger.LogError("A message about the bot reaching an erroneous state")
    /// _logger.LogCritical("A message about a critical exception, usually causing a premature shutdown")
    /// </example>
    private readonly ILogger _logger;
    
    public TeamAi(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<TeamAi>();
    }

    public void CreateContext(StartGameData gameData)
    {
        Context = new TeamAiContext(gameData.TickLength, gameData.TurnRate);
    }

    /// <summary>
    /// Main function defining the behaviour of the AI of the team
    /// </summary>
    /// <param name="gameState">the current state of the game</param>
    /// <returns>A Command instance containing the type and data of the command to be executed on the
    /// tick. Returning None tells server to move 0 steps forward.</returns>
    /// <remarks>You can get tick time in milliseconds from `context.tick_length_ms` and ship turn rate
    /// in 1/8th circles from `context.turn_rate`.
    ///
    /// If your function takes longer than the max tick length the function is cancelled and None is
    /// returned.</remarks>
    public Command? ProcessTick(GameState gameState)
    {
        _logger.LogDebug("Processing tick.");
        
        // Your code goes here
        return null;
    }
    
}