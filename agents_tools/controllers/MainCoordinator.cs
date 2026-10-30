namespace agents_tools.controllers;

using agents_tools.models;

public sealed class MainCoordinator : IDisposable
{
    private readonly AssetCatalogService _catalogService;
    private readonly InstallService _installService;
    private readonly McpServerProcessService _mcpServerService;

    public MainCoordinator(
        AssetCatalogService catalogService,
        InstallService installService,
        McpServerProcessService mcpServerService)
    {
        _catalogService = catalogService;
        _installService = installService;
        _mcpServerService = mcpServerService;
    }

    public CatalogLoadSnapshot LoadCatalog()
    {
        return _catalogService.LoadCatalogSnapshot();
    }

    public Task InstallAsync(CatalogEntry entry, IProgress<InstallProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        return entry.AssetType switch
        {
            CatalogAssetType.Agent => InstallAgentAsync(entry, progress, cancellationToken),
            CatalogAssetType.Tool => InstallToolAsync(entry, progress, cancellationToken),
            _ => throw new InvalidOperationException("Unknown catalog asset type.")
        };
    }

    private async Task InstallAgentAsync(CatalogEntry entry, IProgress<InstallProgress>? progress, CancellationToken cancellationToken)
    {
        var agent = _catalogService.FindAgentById(entry.Id);
        if (agent is null)
        {
            throw new InvalidOperationException("Agent not found.");
        }

        await _installService.InstallAgentAsync(agent, progress, cancellationToken).ConfigureAwait(false);
    }

    private async Task InstallToolAsync(CatalogEntry entry, IProgress<InstallProgress>? progress, CancellationToken cancellationToken)
    {
        var tool = _catalogService.FindToolById(entry.Id);
        if (tool is null)
        {
            throw new InvalidOperationException("Tool not found.");
        }

        await _installService.InstallToolAsync(tool, progress, cancellationToken).ConfigureAwait(false);
    }

    public McpServerStatusSnapshot GetMcpStatus() => _mcpServerService.GetStatus();

    public Task<McpServerStatusSnapshot> StartMcpAsync(CancellationToken cancellationToken = default)
        => _mcpServerService.StartAsync(cancellationToken);

    public Task<McpServerStatusSnapshot> StopMcpAsync(CancellationToken cancellationToken = default)
        => _mcpServerService.StopAsync(cancellationToken);

    public IReadOnlyList<string> ReadRecentMcpLogLines(int maxLines)
        => _mcpServerService.ReadRecentLogLines(maxLines);

    public void ClearMcpLogs() => _mcpServerService.ClearLogs();

    public string McpLogFilePath => _mcpServerService.LogFilePath;

    public void Dispose()
    {
        _mcpServerService.Dispose();
    }
}
