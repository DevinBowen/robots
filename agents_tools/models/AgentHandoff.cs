namespace agents_tools.models;

public sealed class AgentHandoff
{
    public string ToAgentId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
