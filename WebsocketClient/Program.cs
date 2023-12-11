using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebsocketClient.Logging;
using WebsocketClient.Wrapper;
using WebsocketClient.Wrapper.Entities;

class Program
{
    private static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true, true)
            .AddEnvironmentVariables().Build();
        var loggingConfig = config.GetRequiredSection("Logging").Get<AppLoggingConfig>();
        var logFilePath = $"../../../../{loggingConfig.LogFile}";
        await using var logFileWriter = new StreamWriter(logFilePath, append: false);
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            if (loggingConfig.LogToConsole)
            {
                builder.AddConsole();
            }
            builder.AddFilter("WebsocketClient.TeamAi", loggingConfig.TeamAi.LogLevel);
            builder.AddFilter("WebsocketClient.Wrapper", loggingConfig.Wrapper.LogLevel);
            builder.AddProvider(new FileLoggerProvider(logFileWriter));
        });
        var client = new Client(loggerFactory, config["Client:Token"], config["Client:BotName"]);
        await client.Run(config["Client:WebSocketUrl"]);
    }
}