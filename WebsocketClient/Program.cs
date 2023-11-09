// See https://aka.ms/new-console-template for more information

using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;
using WebsocketClient.Logging;

class Program
{
    static async Task Main(string[] args)
    {
        const string logFilePath = "test_log.log";
        await using var logFileWriter = new StreamWriter(logFilePath, append: true);
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.AddProvider(new FileLoggerProvider(logFileWriter));
        });
        var logger = loggerFactory.CreateLogger<Program>();
        var websocketClient = new ClientWebSocket();
        await websocketClient.ConnectAsync(new Uri("ws://localhost:8765"), new CancellationToken());
        await websocketClient.SendAsync(
            "{\"eventType\": \"auth\", \"data\": {\"token\": \"myBotToken\"}}"u8.ToArray(),
            WebSocketMessageType.Text,
            true,
            new CancellationToken());
        var buffer = new byte[1024];
        await websocketClient.ReceiveAsync(buffer, new CancellationToken());
        logger.LogInformation(Encoding.UTF8.GetString(buffer));
        
    }
}