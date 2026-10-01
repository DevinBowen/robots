namespace agents_tools.models;

using System;
using System.Collections.Generic;

public sealed class InstallManifest
{
    public List<InstalledItem> InstalledItems { get; set; } = new();
}

public sealed class InstalledItem
{
    public string AssetId { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty; // "agent" or "tool"
    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;
    public string SourcePath { get; set; } = string.Empty;
}
