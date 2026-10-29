namespace agents_tools.models;

public enum McpServerRunState
{
    Stopped = 0,
    Running = 1
}

public sealed class McpServerLaunchConfig
{
    public string Source { get; init; } = "Default";
    public string TransportType { get; init; } = "stdio";
    public string Command { get; init; } = "dotnet";
    public IReadOnlyList<string> Arguments { get; init; } = Array.Empty<string>();
    public string WorkingDirectory { get; init; } = string.Empty;
    public string? Host { get; init; }
    public int? Port { get; init; }

    public string CommandLineDisplay
    {
        get
        {
            var args = Arguments is null || Arguments.Count == 0
                ? string.Empty
                : " " + string.Join(" ", Arguments.Select(QuoteIfNeeded));
            return Command + args;
        }
    }

    private static string QuoteIfNeeded(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "\"\"";
        }

        return value.Contains(' ') ? $"\"{value}\"" : value;
    }
}

public sealed class McpServerStatusSnapshot
{
    public McpServerRunState State { get; init; }
    public int? ProcessId { get; init; }
    public string Message { get; init; } = string.Empty;
    public McpServerLaunchConfig LaunchConfig { get; init; } = new();
}
