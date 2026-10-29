using SmartComponents.LocalEmbeddings;
using UglyToad.PdfPig;

namespace Rag;

public sealed record SearchResult(string Source, int? Page, string Text, float Score);

public sealed class RagIndex : IDisposable
{
    private sealed record Chunk(string Source, int? Page, string Text, EmbeddingF32 Embedding);

    // TODO(rag): swap for a swappable embedding provider if a larger model is needed.
    private readonly LocalEmbedder _embedder = new();

    // TODO(rag): in-memory only, rebuilt on every start; persist to disk or a vector store when the corpus grows.
    private readonly List<Chunk> _chunks = [];

    public int ChunkCount => _chunks.Count;

    public void IngestFolder(string folder)
    {
        // TODO(rag): add other formats (docx, html) and skip unchanged files.
        foreach (var file in Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            var source = Path.GetRelativePath(folder, file);
            switch (ext)
            {
                case ".txt":
                case ".md":
                    AddText(source, null, File.ReadAllText(file));
                    break;
                case ".pdf":
                    using (var pdf = PdfDocument.Open(file))
                    {
                        foreach (var page in pdf.GetPages())
                            AddText(source, page.Number, page.Text);
                    }
                    break;
            }
        }
    }

    public IReadOnlyList<SearchResult> Search(string query, int topK = 5)
    {
        var q = _embedder.Embed(query);
        // TODO(rag): brute-force cosine scan; fine for thousands of chunks, replace with an ANN index beyond that.
        return _chunks
            .Select(c => new SearchResult(c.Source, c.Page, c.Text, c.Embedding.Similarity(q)))
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .ToList();
    }

    private void AddText(string source, int? page, string text)
    {
        foreach (var piece in Split(text))
            _chunks.Add(new Chunk(source, page, piece, _embedder.Embed(piece)));
    }

    // TODO(rag): fixed-size character chunking; prefer heading/paragraph boundaries and token-based sizes.
    private static IEnumerable<string> Split(string text, int size = 800, int overlap = 100)
    {
        text = text.Trim();
        for (var start = 0; start < text.Length; start += size - overlap)
        {
            var piece = text.Substring(start, Math.Min(size, text.Length - start)).Trim();
            if (piece.Length > 0) yield return piece;
            if (start + size >= text.Length) yield break;
        }
    }

    public void Dispose() => _embedder.Dispose();
}
