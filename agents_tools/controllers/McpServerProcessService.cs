namespace agents_tools.controllers;

using System.Diagnostics;
using System.Text.Json;
using agents_tools.models;

public sealed class McpServerProcessService : IDisposable
{
    private readonly object _gate = new();
    private Process? _process;
    private bool _stopRequested;
    private readonly string _logFilePath;

    public McpServerProcessService()
    {
        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Robots",
            "logs");

        Directory.CreateDirectory(logDirectory);
        _logFilePath = Path.Combine(logDirectory, "mcp-server.log");
    }

    public string LogFilePath => _logFilePath;

    public McpServerLaunchConfig GetLaunchConfiguration()
    {
        return ResolveLaunchConfiguration();
    }

    public McpServerStatusSnapshot GetStatus()
    {
        var launchConfig = ResolveLaunchConfiguration();

        lock (_gate)
        {
            if (_process is not null)
            {
                if (!_process.HasExited)
                {
                    return new McpServerStatusSnapshot
                    {
                        State = McpServerRunState.Running,
                        ProcessId = _process.Id,
                        Message = "Running",
                        LaunchConfig = launchConfig
                    };
                }

                var exitCode = _process.ExitCode;
                _process.Dispose();
                _process = null;

                return new McpServerStatusSnapshot
                {
                    State = McpServerRunState.Stopped,
                    Message = $"Stopped (exit code {exitCode})",
                    LaunchConfig = launchConfig
                };
            }
        }

        return new McpServerStatusSnapshot
        {
            State = McpServerRunState.Stopped,
            Message = "Stopped",
            LaunchConfig = launchConfig
        };
    }

    public async Task<McpServerStatusSnapshot> StartAsync(CancellationToken cancellationToken = default)
    {
        var launchConfig = ResolveLaunchConfiguration();

        lock (_gate)
        {
            if (_process is not null && !_process.HasExited)
            {
                return new McpServerStatusSnapshot
                {
                    State = McpServerRunState.Running,
                    ProcessId = _process.Id,
                    Message = "Running",
                    LaunchConfig = launchConfig
                };
            }
        }

        var processStartInfo = new ProcessStartInfo
        {
            FileName = launchConfig.Command,
            WorkingDirectory = launchConfig.WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var argument in launchConfig.Arguments)
        {
            processStartInfo.ArgumentList.Add(argument);
        }

        var process = new Process { StartInfo = processStartInfo, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) => AppendStreamLog(e.Data, isErrorStream: false);
        process.ErrorDataReceived += (_, e) => AppendStreamLog(e.Data, isErrorStream: true);
        process.Exited += (_, _) =>
        {
            var wasStopRequested = false;
            lock (_gate)
            {
                wasStopRequested = _stopRequested;
                _stopRequested = false;
            }

            if (wasStopRequested)
            {
                AppendLog("MCP server process stopped by request.", "SYS");
            }
            else
            {
                AppendLog($"MCP server process exited with code {process.ExitCode}.", "SYS");
            }
        };

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException("Process start returned false.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            lock (_gate)
            {
                _stopRequested = false;
                _process = process;
            }

            AppendLog($"Started MCP server process (PID {process.Id}).", "SYS");
            await Task.Delay(250, cancellationToken);
            return GetStatus();
        }
        catch (Exception ex)
        {
            process.Dispose();
            AppendLog($"Failed to start MCP server: {ex.Message}", "SYS");
            throw new InvalidOperationException("Failed to start MCP server process.", ex);
        }
    }

    public async Task<McpServerStatusSnapshot> StopAsync(CancellationToken cancellationToken = default)
    {
        Process? runningProcess;

        lock (_gate)
        {
            runningProcess = _process;
            _process = null;
            _stopRequested = runningProcess is not null;
        }

        if (runningProcess is null)
        {
            return GetStatus();
        }

        try
        {
            if (!runningProcess.HasExited)
            {
                AppendLog($"Stopping MCP server process (PID {runningProcess.Id}).", "SYS");
                runningProcess.Kill(entireProcessTree: true);
                await runningProcess.WaitForExitAsync(cancellationToken);
            }
        }
        finally
        {
            runningProcess.Dispose();
        }

        return GetStatus();
    }

    public IReadOnlyList<string> ReadRecentLogLines(int maxLines)
    {
        if (maxLines <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLines), "maxLines must be greater than zero.");
        }

        if (!File.Exists(_logFilePath))
        {
            return Array.Empty<string>();
        }

        try
        {
            var queue = new Queue<string>();
            foreach (var line in File.ReadLines(_logFilePath))
            {
                queue.Enqueue(line);
                if (queue.Count > maxLines)
                {
                    queue.Dequeue();
                }
            }

            return queue.ToArray();
        }
        catch (IOException ex)
        {
            return new[] { $"[Log read failed] {ex.Message}" };
        }
    }

    public void ClearLogs()
    {
        File.WriteAllText(_logFilePath, string.Empty);
    }

    private void AppendLog(string? line, string category)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        var timestamp = DateTimeOffset.Now.ToString("u");
        var text = $"[{timestamp}] [{category}] {line}{Environment.NewLine}";

        lock (_gate)
        {
            File.AppendAllText(_logFilePath, text);
        }
    }

    private void AppendStreamLog(string? line, bool isErrorStream)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        if (!isErrorStream)
        {
            AppendLog(line, "STDOUT");
            return;
        }

        var category = ClassifyStandardErrorLine(line);
        AppendLog(line, category);
    }

    private static string ClassifyStandardErrorLine(string line)
    {
        if (line.StartsWith("info:", StringComparison.OrdinalIgnoreCase))
        {
            return "INFO";
        }

        if (line.StartsWith("warn:", StringComparison.OrdinalIgnoreCase))
        {
            return "WARN";
        }

        if (line.StartsWith("fail:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("crit:", StringComparison.OrdinalIgnoreCase))
        {
            return "ERROR";
        }

        return "STDERR";
    }

    private static McpServerLaunchConfig ResolveLaunchConfiguration()
    {
        var solutionRoot = FindSolutionRoot();
        var configCandidates = new[]
        {
            Path.Combine(solutionRoot, ".mcp.json"),
            Path.Combine(solutionRoot, ".vscode", "mcp.json")
        };

        foreach (var candidate in configCandidates)
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            try
            {
                if (TryReadConfig(candidate, solutionRoot, out var config))
                {
                    return config;
                }
            }
            catch (JsonException)
            {
                // Ignore malformed config files and continue to fallback.
            }
            catch (IOException)
            {
                // Ignore transient file access issues and continue to fallback.
            }
        }

        var defaultProjectPath = Path.Combine(solutionRoot, "McpServer", "McpServer.csproj");
        return new McpServerLaunchConfig
        {
            Source = "Default",
            TransportType = "stdio",
            Command = "dotnet",
            Arguments = new[] { "run", "--project", defaultProjectPath },
            WorkingDirectory = solutionRoot
        };
    }

    private static bool TryReadConfig(string configPath, string solutionRoot, out McpServerLaunchConfig config)
    {
        using var stream = File.OpenRead(configPath);
        using var document = JsonDocument.Parse(stream);

        if (!document.RootElement.TryGetProperty("servers", out var servers) || servers.ValueKind != JsonValueKind.Object)
        {
            config = default!;
            return false;
        }

        JsonElement? selectedServer = null;
        foreach (var serverProperty in servers.EnumerateObject())
        {
            if (string.Equals(serverProperty.Name, "McpServer", StringComparison.OrdinalIgnoreCase))
            {
                selectedServer = serverProperty.Value;
                break;
            }

            selectedServer ??= serverProperty.Value;
        }

        if (selectedServer is null || selectedServer.Value.ValueKind != JsonValueKind.Object)
        {
            config = default!;
            return false;
        }

        var server = selectedServer.Value;
        var command = ReadString(server, "command") ?? "dotnet";
        var transport = ReadString(server, "type") ?? "stdio";
        var workingDirectory = ReadString(server, "cwd");

        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            workingDirectory = Path.GetDirectoryName(configPath) ?? solutionRoot;
        }

        var args = ReadStringArray(server, "args");

        string? host = ReadString(server, "host");
        int? port = ReadInt(server, "port");

        config = new McpServerLaunchConfig
        {
            Source = configPath,
            TransportType = transport,
            Command = command,
            Arguments = args,
            WorkingDirectory = workingDirectory,
            Host = host,
            Port = port
        };

        return true;
    }

    private static string[] ReadStringArray(JsonElement node, string propertyName)
    {
        if (!node.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        var result = new List<string>();
        foreach (var item in property.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var value = item.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    result.Add(value);
                }
            }
        }

        return result.ToArray();
    }

    private static string? ReadString(JsonElement node, string propertyName)
    {
        if (!node.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return property.GetString();
    }

    private static int? ReadInt(JsonElement node, string propertyName)
    {
        if (!node.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return property.TryGetInt32(out var value) ? value : null;
    }

    private static string FindSolutionRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var slnx = Path.Combine(current.FullName, "robots.slnx");
            if (File.Exists(slnx))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return AppContext.BaseDirectory;
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _process?.Dispose();
            _process = null;
        }
    }
}
