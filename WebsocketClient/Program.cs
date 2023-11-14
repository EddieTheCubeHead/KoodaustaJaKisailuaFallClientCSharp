// See https://aka.ms/new-console-template for more information

using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;
using WebsocketClient.Logging;
using WebsocketClient.Wrapper;

class Program
{
    private static async Task Main(string[] args)
    {
        const string logFilePath = "../../../../test_log.log";
        await using var logFileWriter = new StreamWriter(logFilePath, append: true);
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.AddProvider(new FileLoggerProvider(logFileWriter));
        });
        var websocketClient = new ClientWebSocket();
        await websocketClient.ConnectAsync(new Uri("ws://localhost:8765"), new CancellationToken());
        var client = new Client(websocketClient, loggerFactory);
        await client.Run();
    }
}