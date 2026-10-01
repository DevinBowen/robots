namespace agents_tools.models;

using System.Collections.Generic;

public sealed class ToolManifest
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Capabilities { get; set; } = new();
    public List<string> Commands { get; set; } = new();
    public string SourcePath { get; set; } = string.Empty;
}
