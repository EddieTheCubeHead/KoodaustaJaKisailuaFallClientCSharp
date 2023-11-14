using System.ComponentModel;
using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WebsocketClient.Wrapper.Entities;

namespace WebsocketClient.Wrapper;

public class Client
{
    private ILogger _logger;
    private WebSocket _webSocket;
    private TeamAi _teamAi = new TeamAi();
    private Serializer _serializer = new Serializer();
    public ClientState State { get; private set; } = ClientState.Unauthorized;

    public Client(WebSocket webSocket, ILoggerFactory loggerFactory)
    {
        _webSocket = webSocket;
        _logger = loggerFactory.CreateLogger<Client>();
    }
    
    public async Task Run()
    {
        await Authenticate();
        while (true)
        {
            await listenOnWebsocket();
        }
    }
    
    private async Task Authenticate()
    {
        var token = "replaceLaterWithConfigFetch";
        var name = "replaceLaterWithConfigFetch";
        var authCommand = $"{{\"eventType\": \"auth\", \"data\": {{\"token\": \"{token}\", \"botName\": \"{name}\"}}}}";
        _logger.LogInformation("Authenticating...");
        await _webSocket.SendAsync(
            Encoding.UTF8.GetBytes(authCommand),
            WebSocketMessageType.Text, true, new CancellationToken());
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
        _logger.LogInformation("Waiting for message...");
        var buffer = new ArraySegment<byte>(new byte[1024]);
        var result = await _webSocket.ReceiveAsync(buffer, new CancellationToken());
        if (buffer.Array is null)
        {
            _logger.LogError("Received null message");
            return string.Empty;
        }

        var rawMessage = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);

        _logger.LogInformation($"Received message: {rawMessage}");
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

        await _webSocket.SendAsync("{\"eventType\": \"startAck\", \"data\": {}}"u8.ToArray(),
                WebSocketMessageType.Text, true, new CancellationToken());
    }
    
    private async Task HandleGameTick(GameState gameState)
    {
        _logger.LogInformation($"Received game tick of turn {gameState.TurnNumber}");
        var command = _teamAi.ProcessTick(gameState);
        if (command is null)
        {
            _logger.LogWarning("Received null command from TeamAi");
            return;
        }
        await _webSocket.SendAsync(
            Encoding.UTF8.GetBytes(_serializer.SerializeCommand(command)),
            WebSocketMessageType.Text, true, new CancellationToken());
    }
    
    private async Task HandleGameEnd()
    {
        if (State != ClientState.Idle)
        {
            _logger.LogWarning("Received game end while not in in gmae state");
            return;
        }

        await _webSocket.SendAsync("{\"eventType\": \"endAck\", \"data\": {}}"u8.ToArray(),
            WebSocketMessageType.Text, true, new CancellationToken());
    }
}