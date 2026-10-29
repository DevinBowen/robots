using System.ComponentModel;
using ModelContextProtocol.Server;
using Rag;

[McpServerToolType]
public sealed class RagTools(RagIndex index)
{
    [McpServerTool(Name = "search_knowledge")]
    [Description("Searches the indexed documents (e.g. truck maintenance manuals) and returns the most relevant passages.")]
    public string SearchKnowledge(
        [Description("Natural language question or keywords.")] string query,
        [Description("Number of passages to return.")] int topK = 5)
    {
        var results = index.Search(query, topK);
        return string.Join("\n\n---\n\n", results.Select(r =>
            $"[{r.Source}{(r.Page is null ? "" : $" p.{r.Page}")}] (score {r.Score:F2})\n{r.Text}"));
    }
}
