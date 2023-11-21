using Microsoft.Extensions.Logging;

namespace WebsocketClient.Wrapper.Entities;

public class AppLoggingConfig
{
    public LoggingFilterConfig Wrapper { get; set; } = new();
    public LoggingFilterConfig TeamAi { get; set; } = new();
    public string LogFile { get; set; } = "wrapper.log";
    public bool LogToConsole { get; set; } = true;
}

public class LoggingFilterConfig
{
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
}