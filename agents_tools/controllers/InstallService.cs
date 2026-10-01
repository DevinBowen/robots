namespace agents_tools.controllers;

using System;
using System.IO;
using System.Text.Json;
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

        // avoid duplicates by AssetId + AssetType
        var exists = manifest.InstalledItems.Exists(i => string.Equals(i.AssetId, item.AssetId, StringComparison.OrdinalIgnoreCase)
                                                         && string.Equals(i.AssetType, item.AssetType, StringComparison.OrdinalIgnoreCase));
        if (!exists)
        {
            manifest.InstalledItems.Add(item);
            var outJson = JsonSerializer.Serialize(manifest, JsonOptions);
            File.WriteAllText(path, outJson);
        }
    }
}
