using System.ComponentModel;
using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WebsocketClient.Entities;
using WebsocketClient.Wrapper.Entities;

namespace WebsocketClient.Wrapper;

public class Client
{
    private ClientWebSocket? _webSocket;
    private readonly ILogger _logger;
    private readonly TeamAi _teamAi;
    private readonly Serializer _serializer = new();
    private ClientState State { get; set; } = ClientState.Unauthorized;

    public Client(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Client>();
        _teamAi = new TeamAi(loggerFactory);
    }
    
    public async Task Run(string socketUri, string token, string botName)
    {
        await Connect(socketUri, token, botName);
        await Authenticate(token, botName);
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
        var buffer = new ArraySegment<byte>(new byte[1024]);
        var result = await _webSocket.ReceiveAsync(buffer, new CancellationToken());
        if (buffer.Array is null)
        {
            _logger.LogError("Received null message");
            return string.Empty;
        }

        var rawMessage = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);

        _logger.LogDebug($"Received message: {rawMessage}");
        return rawMessage;
    }

    private (ServerEvent? eventType, GameState? gameState) ParseRawMessage(string rawMessage)
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

        return serverEvent == ServerEvent.GameTick
            ? (serverEvent, (GameState)_serializer.DeserializeGameState(partlyParsedMessage.data))
            : (serverEvent, null);
    }

    private async Task HandleEvent(ServerEvent? eventType, GameState? gameState)
    {
        switch (eventType)
        {
            case ServerEvent.AuthAck:
                HandleAuthAck();
                break;
            case ServerEvent.StartGame:
                await HandleGameStart();
                break;
            case ServerEvent.GameTick:
                if (gameState is null)
                {
                    _logger.LogWarning("Received null game state on game tick handling");
                    return;
                }
                await HandleGameTick(gameState);
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

    private async Task HandleGameStart()
    {
        if (State != ClientState.Idle)
        {
            _logger.LogWarning("Received game start while not in idle state");
            return;
        }
        
        _teamAi.ResetContext();

        await SendMessage("startAck", "{}");
    }
    
    private async Task HandleGameTick(GameState gameState)
    {
        _logger.LogDebug($"Received game tick of turn {gameState.TurnNumber}");
        var task = Task.Run(() => _teamAi.ProcessTick(gameState));
        Command? command = null;
        if (task.Wait(TimeSpan.FromMilliseconds(400)))
        {
            command = task.Result;
        }
        else
        {
            _logger.LogWarning("TeamAi took too long to process tick");
        }
        command ??= new Command { Action = ActionType.Move, ActionData = new MoveActionData { Distance = 0 } };
        await SendMessage("gameAction", _serializer.SerializeCommand(command));
    }
    
    private async Task HandleGameEnd()
    {
        if (State != ClientState.Idle)
        {
            _logger.LogWarning("Received game end while not in in game state");
            return;
        }
        
        _teamAi.ResetContext();

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