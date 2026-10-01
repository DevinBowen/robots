namespace agents_tools.controllers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using agents_tools.models;

public sealed class AssetCatalogService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public IReadOnlyList<AgentManifest> GetAgents()
        => ReadManifests<AgentManifest>(Path.Combine(GetAssetsRoot(), "agents"));

    public IReadOnlyList<ToolManifest> GetTools()
        => ReadManifests<ToolManifest>(Path.Combine(GetAssetsRoot(), "tools"));

    public AgentManifest? FindAgentById(string id)
        => GetAgents().FirstOrDefault(a => string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase));

    public ToolManifest? FindToolById(string id)
        => GetTools().FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));

    private static List<T> ReadManifests<T>(string folderPath)
    {
        var results = new List<T>();

        if (!Directory.Exists(folderPath))
        {
            return results;
        }

        var files = Directory.GetFiles(folderPath, "*.json", SearchOption.TopDirectoryOnly);
        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var model = JsonSerializer.Deserialize<T>(json, JsonOptions);
                if (model is not null)
                {
                    // If model has SourcePath property, set it via reflection for diagnostics (best-effort)
                    var prop = typeof(T).GetProperty("SourcePath");
                    if (prop is not null && prop.CanWrite)
                    {
                        prop.SetValue(model, file);
                    }

                    results.Add(model);
                }
            }
            catch
            {
                // skip malformed files
            }
        }

        return results;
    }

    private static string GetAssetsRoot()
    {
        // Prefer runtime output path
        var outputPath = Path.Combine(AppContext.BaseDirectory, "assets");
        if (Directory.Exists(outputPath))
        {
            return outputPath;
        }

        // Developer fallback (project relative)
        var projectPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "assets"));
        return projectPath;
    }
}
