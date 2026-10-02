namespace agents_tools.controllers;

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using agents_tools.models;

public sealed class InstallService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private static string GetManifestPath()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Robots");
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        return Path.Combine(folder, "installed_assets.json");
    }

    public void AppendInstalledItem(InstalledItem item)
    {
        var path = GetManifestPath();
        InstallManifest manifest;

        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path);
                manifest = JsonSerializer.Deserialize<InstallManifest>(json, JsonOptions) ?? new InstallManifest();
            }
            catch
            {
                manifest = new InstallManifest();
            }
        }
        else
        {
            manifest = new InstallManifest();
        }

        // replace any existing entry for AssetId + AssetType
        manifest.InstalledItems.RemoveAll(i => string.Equals(i.AssetId, item.AssetId, StringComparison.OrdinalIgnoreCase)
                                               && string.Equals(i.AssetType, item.AssetType, StringComparison.OrdinalIgnoreCase));
        manifest.InstalledItems.Add(item);
        var outJson = JsonSerializer.Serialize(manifest, JsonOptions);
        File.WriteAllText(path, outJson);
    }

    private static string ToFileStem(string id, string prefix)
    {
        var stem = id ?? string.Empty;
        var lead = prefix + ".";
        if (stem.StartsWith(lead, StringComparison.OrdinalIgnoreCase)) stem = stem.Substring(lead.Length);
        foreach (var c in Path.GetInvalidFileNameChars()) stem = stem.Replace(c, '-');
        return stem;
    }

    private static string GetInstallRoot()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var folder = Path.Combine(userProfile, ".github", "agents");
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        return folder;
    }

    public async Task InstallAgentAsync(AgentManifest agent, IProgress<InstallProgress>? progress = null, CancellationToken cancellation = default)
    {
        if (agent is null) throw new ArgumentNullException(nameof(agent));

        var root = GetInstallRoot();

        // Generate markdown content
        var md = MarkdownGenerator.GenerateAgentMarkdown(agent);
        var mdBytes = Encoding.UTF8.GetBytes(md);

        // Write md directly to root with progress
        var destMd = Path.Combine(root, ToFileStem(agent.Id, "agent") + ".agent.md");
        long written = 0;
        long totalBytes = mdBytes.Length;

        await WriteBytesWithProgressAsync(destMd, mdBytes, (b) =>
        {
            written += b;
            progress?.Report(new InstallProgress { BytesWritten = written, TotalBytes = totalBytes });
        }, cancellation).ConfigureAwait(false);

        // record installation
        AppendInstalledItem(new InstalledItem
        {
            AssetId = agent.Id,
            AssetType = "agent",
            InstalledAt = DateTime.UtcNow,
            SourcePath = destMd
        });
    }

    public async Task InstallToolAsync(ToolManifest tool, IProgress<InstallProgress>? progress = null, CancellationToken cancellation = default)
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));

        var root = GetInstallRoot();

        // Generate markdown content
        var md = MarkdownGenerator.GenerateToolMarkdown(tool);
        var mdBytes = Encoding.UTF8.GetBytes(md);

        // Write md directly to root with progress
        var destMd = Path.Combine(root, ToFileStem(tool.Id, "tool") + ".tool.md");
        long written = 0;
        long totalBytes = mdBytes.Length;

        await WriteBytesWithProgressAsync(destMd, mdBytes, (b) =>
        {
            written += b;
            progress?.Report(new InstallProgress { BytesWritten = written, TotalBytes = totalBytes });
        }, cancellation).ConfigureAwait(false);

        // record installation
        AppendInstalledItem(new InstalledItem
        {
            AssetId = tool.Id,
            AssetType = "tool",
            InstalledAt = DateTime.UtcNow,
            SourcePath = destMd
        });
    }

    private static async Task WriteBytesWithProgressAsync(string destPath, byte[] bytes, Action<long> onBytesWritten, CancellationToken cancellation)
    {
        const int chunk = 81920;
        using var dest = File.Create(destPath);
        int offset = 0;
        while (offset < bytes.Length)
        {
            var toWrite = Math.Min(chunk, bytes.Length - offset);
            await dest.WriteAsync(bytes, offset, toWrite, cancellation).ConfigureAwait(false);
            offset += toWrite;
            onBytesWritten?.Invoke(toWrite);
        }
    }
}
