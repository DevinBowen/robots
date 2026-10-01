namespace agents_tools.models;

using System.Collections.Generic;

public sealed class AgentManifest
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> SupportedActions { get; set; } = new();
    public List<string> ToolIds { get; set; } = new();
    public List<AgentHandoff> Handoffs { get; set; } = new();
    // Optional: path to the source json file for diagnostics
    public string SourcePath { get; set; } = string.Empty;
}
