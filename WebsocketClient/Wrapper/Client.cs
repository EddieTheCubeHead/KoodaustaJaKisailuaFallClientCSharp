using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WebsocketClient.Wrapper.Entities;

namespace WebsocketClient.Wrapper;

public class Client
{
    private ClientWebSocket? _webSocket;
    private readonly ILogger _logger;
    private readonly TeamAi _teamAi;
    private readonly Serializer _serializer = new();
    private readonly string _token;
    private readonly string _botName;
    private ClientState State { get; set; } = ClientState.Unauthorized;

    public Client(ILoggerFactory loggerFactory, string token, string botName)
    {
        _logger = loggerFactory.CreateLogger<Client>();
        _token = token;
        _botName = botName;
        _teamAi = new TeamAi(loggerFactory, token, botName);
    }

    public async Task Run(string socketUri)
    {
        await Connect(socketUri, _token, _botName);
        await Authenticate(_token, _botName);
        while (true)
        {
            await listenOnWebsocket();
        }
        // ReSharper disable once FunctionNeverReturns : endless loop to run the client
    }

    private async Task Connect(string socketAddress, string token, string botName)
    {
        _webSocket = new ClientWebSocket();
        var fullUriString = $"{socketAddress}?token={token}&botName={botName}";
        _logger.LogInformation($"Connecting to {fullUriString}");
        await _webSocket.ConnectAsync(new Uri(fullUriString), new CancellationToken());
    }

    private async Task Authenticate(string token, string botName)
    {
        _logger.LogInformation($"Authorizing client with token '{token}' and name '{botName}'.");
        var authCommand =
            $"{{\"token\": \"{token}\", \"botName\": \"{botName}\"}}";
        await SendMessage("auth", authCommand);
    }

    private async Task listenOnWebsocket()
    {
        var rawMessage = await WaitForRawMessage();
        var (eventType, data) = ParseRawMessage(rawMessage);
        if (eventType is null)
        {
            return;
        }

        await HandleEvent(eventType, data);
    }

    private async Task<string> WaitForRawMessage()
    {
        _logger.LogDebug("Waiting for message...");
        var rawMessage = string.Empty;
        WebSocketReceiveResult result;
        do
        {
            var buffer = new ArraySegment<byte>(new byte[1024]);
            result = await _webSocket.ReceiveAsync(buffer, new CancellationToken());
            if (buffer.Array is null)
            {
                _logger.LogError("Received null message");
                return string.Empty;
            }
            rawMessage += Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
        } while (!result.EndOfMessage);

        _logger.LogDebug($"Received message: {rawMessage}");
        return rawMessage;
    }

    private (ServerEvent? eventType, dynamic? eventData) ParseRawMessage(string rawMessage)
    {
        if (string.IsNullOrEmpty(rawMessage))
        {
            return (null, null);
        }
        dynamic? partlyParsedMessage = JsonConvert.DeserializeObject(rawMessage);
        if (partlyParsedMessage is null)
        {
            _logger.LogError($"Could not parse message '{rawMessage}'");
            return (null, null);
        }

        if (!Enum.TryParse((string)partlyParsedMessage.eventType, true, out ServerEvent serverEvent))
        {
            _logger.LogError($"Could not parse event type from '{partlyParsedMessage.eventType}'");
            return (null, null);
        }

        return (serverEvent, partlyParsedMessage.data);
    }

    private async Task HandleEvent(ServerEvent? eventType, dynamic? eventData)
    {
        switch (eventType)
        {
            case ServerEvent.AuthAck:
                HandleAuthAck();
                break;
            case ServerEvent.StartGame:
                await HandleGameStart(eventData);
                break;
            case ServerEvent.GameTick:
                await HandleGameTick(eventData);
                break;
            case ServerEvent.EndGame:
                await HandleGameEnd();
                break;
            case null:
                _logger.LogWarning("Received null event type in HandleEvent");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(eventType), eventType, null);
        }
    }

    private void HandleAuthAck()
    {
        if (State == ClientState.Unauthorized)
        {
            State = ClientState.Idle;
            _logger.LogInformation("Authorization successful");
        }
    }

    private async Task HandleGameStart(dynamic? rawGameData)
    {
        if (State != ClientState.Idle)
        {
            _logger.LogWarning("Received game start while not in idle state");
            return;
        }

        StartGameData gameData = _serializer.DeserializeStartGameData(rawGameData);

        _teamAi.CreateContext(gameData);

        await SendMessage("startAck", "{}");
    }

    private async Task HandleGameTick(dynamic? rawGameState)
    {
        if (rawGameState is null)
        {
            _logger.LogWarning("Received null game state data on game tick handling");
            return;
        }

        GameState gameState = _serializer.DeserializeGameState(rawGameState);
        _logger.LogDebug($"Received game tick of turn {gameState.TurnNumber}");
        var command = HandleTickWithTimeout(gameState) ?? new Command
        { Action = ActionType.Move, Payload = new MoveActionData { Distance = 0 } };
        await SendMessage("gameAction", _serializer.SerializeCommand(command));
    }

    private Command? HandleTickWithTimeout(GameState gameState)
    {
        if (_teamAi.Context.TickLength == 0)
        {
            return ProcessTickWrapper(gameState);
        }

        //Command? command = ProcessTickWrapper(gameState);

        Command? command = null;
        var task = Task.Run(() => ProcessTickWrapper(gameState));

        if (task.Wait(TimeSpan.FromMilliseconds((int)(_teamAi.Context.TickLength / 2) - 3)))
        {
            command = task.Result;
        }
        else
        {
            _logger.LogWarning($"TeamAi took too long to process tick. Limit: {(int)(_teamAi.Context.TickLength / 2) - 3}ms");
        }

        return command;
    }

    private Command? ProcessTickWrapper(GameState gameState)
    {
        Command? command = null;
        try
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            command = _teamAi.ProcessTick(gameState);
            timer.Stop();
            _logger.LogInformation($"tick processed in {timer.ElapsedMilliseconds} milliseconds");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "TeamAi threw an exception while processing tick");
        }

        return command;
    }

    private async Task HandleGameEnd()
    {
        if (State != ClientState.Idle)
        {
            _logger.LogWarning("Received game end while not in in game state");
            return;
        }

        await SendMessage("endAck", "{}");
    }

    private async Task SendMessage(string eventType, string data)
    {
        if (_webSocket is null)
        {
            _logger.LogError("Attempting to send message before websocket initialization!");
            return;
        }

        var fullMessage = $"{{\"eventType\": \"{eventType}\", \"data\": {data}}}";
        _logger.LogDebug($"Sending: {fullMessage}");
        await _webSocket.SendAsync(Encoding.UTF8.GetBytes(fullMessage), WebSocketMessageType.Text, true,
            new CancellationToken());
    }
}