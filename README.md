# KoodaustaJaKisailua2023FallClient
Client helper/wrapper for the koodausta ja kisailua 2023 fall event.

## Setup

### Configs

Configs can be edited in `WebsocketClient/appsettings.json`. The configs here can
also be supplied as environment variables, if you for example want to create
multiple run configurations in Rider or Visual Studio.

The wrapper has the following configuration values:

 - `Client:WebSocketUrl`: the url of the game server websocket. Already configured
in the repository.
 - `Client:Token`: the unique token identifying your team. Already configured in the 
repository.
 - `Client:BotName`: the name of this bot. Is used to differentiate different bots from
the same team.
 - `Logging:LogFile`: the file into which the wrapper writes its logs. Can be
null to prevent wrapper from writing logs into a file. Default 'wrapper.log'.
Doesn't need to be identical to team AI log file.
 - `Logging:LogToConsole`: A boolean value determining whether to write logs to console.
Default true.
 - `Logging:WrapperLogLevel`: the minimum level of log entries to write from wrapper
logging. From least critical to most critical level, the options are 'Trace', 'Debug',
'Info', 'Warning', 'Error', 'Critical' and 'Off'. Default 'Info'. If you want more
verbose feedback about the wrapper during runtime it's recommended to set this to
'Debug'.
 - `Logging:TeamAi:LogLevel`: the minimum level of log entries to write from wrapper
logging. From least critical to most critical level, the options are 'Trace', 'Debug',
   'Info', 'Warning', 'Error', 'Critical' and 'Off'. Default 'Debug'.

## Running

To run the client websocket wrapper, run the solution file `KoodaustaJaKisailuaFallClientCs.sln` 
in the repository root folder.

## Editing the AI function

The AI function can be found in `WebsocketClient/TeamAi`. As the method `TeamAi.ProcessTick` It 
gets the `gameState` parameter from the wrapper. It returns a `Command` object, or `null`. If 
`null` is returned, it will be treated as a "move 0" command.

`gameState` is the present state of the game as seen by the ship.

To store persistent data between ticks, the TeamAi class housing the process tick method has the 
attribute `Context`. It is a persistent object you can use to store data in. Context is wiped
at the start of a match, but preserved during the match. This means you are free
to add data to it to keep the data available through the match. When you need to add new data to the
context or edit the current data, you can edit the `TeamAiContext` class in
`WebsocketClient/Wrapper/Entities/TeamAiContext.cs`. By default `TeamAi` has two members.
`TickLength` holds the value for max tick length in milliseconds. If you function takes longer
than max length - 50 milliseconds, the wrapper will return move 0 steps automatically.
`TurnRate` holds the maximum turn rate of the ship. The rate is given in 1/8ths of a
circle, so each value represents being able to turn one compass direction.

`Command` object should contain the type of the action the bot should perform (`Move`,
`Turn` or `Shoot`) and the action data payload.

If you want to log the behaviour inside the function you can use the `TeamAi._logger`
object. Use methods `_logger.LogDebug("message")`, `_logger.LogInfo("message")`,
`_logger.LogWarning("message")`, `_logger.LogError("message")` and
`_logger.LogCritical("message")` for the different levels of urgency in the log
messages.

Please note that there is a timeout of (tick_time - 50) milliseconds for the
function. This ensures the wrapper can send a command every tick.

# Models

Model data can be found in [MODELS.md](MODELS.md)

# Game loop

Game loop is described in [GAME_REFERENCE.md](GAME_REFERENCE.md)


