using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebsocketClient.Logging;
using WebsocketClient.Wrapper;

class Program
{
    private static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true).Build();
        const string logFilePath = "../../../../test_log.log";
        await using var logFileWriter = new StreamWriter(logFilePath, append: false);
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.AddProvider(new FileLoggerProvider(logFileWriter));
        });
        var client = new Client(loggerFactory);
        await client.Run(config["Client:WebSocketUrl"], config["Client:Token"], config["Client:BotName"]);
    }
}