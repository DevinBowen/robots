namespace agents_tools.models;

public enum CatalogAssetType
{
    Agent = 0,
    Tool = 1
}

public sealed class CatalogEntry
{
    public CatalogAssetType AssetType { get; init; }
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;

    public string DisplayPrefix => AssetType == CatalogAssetType.Agent ? "Agent" : "Tool";
    public string DisplayName => $"{DisplayPrefix}: {Name}";
}

public sealed class CatalogDiagnostic
{
    public CatalogAssetType AssetType { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;

    public override string ToString()
        => $"{AssetType}: {Path.GetFileName(FilePath)} - {Message}";
}

public sealed class CatalogLoadSnapshot
{
    public IReadOnlyList<CatalogEntry> Entries { get; init; } = Array.Empty<CatalogEntry>();
    public IReadOnlyList<CatalogDiagnostic> Diagnostics { get; init; } = Array.Empty<CatalogDiagnostic>();
}
