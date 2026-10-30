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

    public CatalogLoadSnapshot LoadCatalogSnapshot()
    {
        var diagnostics = new List<CatalogDiagnostic>();
        var agents = ReadAgentManifests(Path.Combine(GetAssetsRoot(), "agents"), diagnostics);
        var tools = ReadToolManifests(Path.Combine(GetAssetsRoot(), "tools"), diagnostics);

        var entries = new List<CatalogEntry>(agents.Count + tools.Count);
        entries.AddRange(agents.Select(a => new CatalogEntry
        {
            AssetType = CatalogAssetType.Agent,
            Id = a.Id,
            Name = a.Name,
            Description = a.Description,
            Category = a.Category,
            SourcePath = a.SourcePath
        }));

        entries.AddRange(tools.Select(t => new CatalogEntry
        {
            AssetType = CatalogAssetType.Tool,
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            Category = "Tool",
            SourcePath = t.SourcePath
        }));

        return new CatalogLoadSnapshot
        {
            Entries = entries,
            Diagnostics = diagnostics
        };
    }

    public IReadOnlyList<AgentManifest> GetAgents()
        => ReadAgentManifests(Path.Combine(GetAssetsRoot(), "agents"), diagnostics: null);

    public IReadOnlyList<ToolManifest> GetTools()
        => ReadToolManifests(Path.Combine(GetAssetsRoot(), "tools"), diagnostics: null);

    public AgentManifest? FindAgentById(string id)
        => GetAgents().FirstOrDefault(a => string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase));

    public ToolManifest? FindToolById(string id)
        => GetTools().FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));

    private static List<AgentManifest> ReadAgentManifests(string folderPath, List<CatalogDiagnostic>? diagnostics)
    {
        return ReadManifests<AgentManifest>(folderPath, diagnostics, CatalogAssetType.Agent, ValidateAgent);
    }

    private static List<ToolManifest> ReadToolManifests(string folderPath, List<CatalogDiagnostic>? diagnostics)
    {
        return ReadManifests<ToolManifest>(folderPath, diagnostics, CatalogAssetType.Tool, ValidateTool);
    }

    private static List<TManifest> ReadManifests<TManifest>(
        string folderPath,
        List<CatalogDiagnostic>? diagnostics,
        CatalogAssetType assetType,
        Func<TManifest, string?> validator)
    {
        var results = new List<TManifest>();

        if (!Directory.Exists(folderPath))
        {
            diagnostics?.Add(new CatalogDiagnostic
            {
                AssetType = assetType,
                FilePath = folderPath,
                Message = "Assets folder was not found."
            });
            return results;
        }

        var files = Directory.GetFiles(folderPath, "*.json", SearchOption.TopDirectoryOnly);
        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var model = JsonSerializer.Deserialize<TManifest>(json, JsonOptions);
                if (model is null)
                {
                    diagnostics?.Add(new CatalogDiagnostic
                    {
                        AssetType = assetType,
                        FilePath = file,
                        Message = "Manifest could not be deserialized."
                    });
                    continue;
                }

                var validation = validator(model);
                if (!string.IsNullOrWhiteSpace(validation))
                {
                    diagnostics?.Add(new CatalogDiagnostic
                    {
                        AssetType = assetType,
                        FilePath = file,
                        Message = validation
                    });
                    continue;
                }

                var prop = typeof(TManifest).GetProperty("SourcePath");
                if (prop is not null && prop.CanWrite)
                {
                    prop.SetValue(model, file);
                }

                results.Add(model);
            }
            catch (JsonException ex)
            {
                diagnostics?.Add(new CatalogDiagnostic
                {
                    AssetType = assetType,
                    FilePath = file,
                    Message = $"Invalid JSON: {ex.Message}"
                });
            }
            catch (Exception ex)
            {
                diagnostics?.Add(new CatalogDiagnostic
                {
                    AssetType = assetType,
                    FilePath = file,
                    Message = ex.Message
                });
            }
        }

        return results;
    }

    private static string? ValidateAgent(AgentManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(manifest.Id))
        {
            return "Missing required field 'id'.";
        }

        if (string.IsNullOrWhiteSpace(manifest.Name))
        {
            return "Missing required field 'name'.";
        }

        return null;
    }

    private static string? ValidateTool(ToolManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(manifest.Id))
        {
            return "Missing required field 'id'.";
        }

        if (string.IsNullOrWhiteSpace(manifest.Name))
        {
            return "Missing required field 'name'.";
        }

        return null;
    }

    private static string GetAssetsRoot()
    {
        var outputPath = Path.Combine(AppContext.BaseDirectory, "assets");
        if (Directory.Exists(outputPath))
        {
            return outputPath;
        }

        var projectPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "assets"));
        return projectPath;
    }
}
