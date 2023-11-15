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
        await using var logFileWriter = new StreamWriter(logFilePath, append: false);
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.AddProvider(new FileLoggerProvider(logFileWriter));
        });
        var uriString = "ws://localhost:8765";
        var client = new Client(loggerFactory);
        await client.Run(uriString, "myBotToken", "myBotName");
    }
}