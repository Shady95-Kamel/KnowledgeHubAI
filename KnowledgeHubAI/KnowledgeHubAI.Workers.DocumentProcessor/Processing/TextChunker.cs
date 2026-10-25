
namespace KnowledgeHubAI.Workers.DocumentProcessor.Processing;

public sealed class TextChunker
{
    private const int DefaultChunkSize = 1200;
    private const int DefaultOverlap = 150;

    public IReadOnlyList<DocumentTextChunk> CreateChunks(
        Guid documentId,
        Guid userId,
        IEnumerable<(int PageNumber, string Text)> pages)
    {
        ArgumentNullException.ThrowIfNull(pages);

        var chunks = new List<DocumentTextChunk>();
        var chunkIndex = 0;

        foreach (var page in pages.OrderBy(p => p.PageNumber))
        {
            if (string.IsNullOrWhiteSpace(page.Text))
            {
                continue;
            }

            var text = NormalizeText(page.Text);
            var start = 0;

            while (start < text.Length)
            {
                var length = Math.Min(
                    DefaultChunkSize,
                    text.Length - start);

                var end = start + length;

                // حاول عدم قطع الكلمة في منتصفها.
                if (end < text.Length)
                {
                    var lastSpace = text.LastIndexOf(
                        ' ',
                        end - 1,
                        length);

                    if (lastSpace > start)
                    {
                        end = lastSpace;
                    }
                }

                var content = text[start..end].Trim();

                if (!string.IsNullOrWhiteSpace(content))
                {
                    chunks.Add(new DocumentTextChunk(
                        DocumentId: documentId,
                        UserId: userId,
                        ChunkIndex: chunkIndex++,
                        PageNumber: page.PageNumber,
                        Content: content));
                }

                if (end >= text.Length)
                {
                    break;
                }

                // تداخل بين الأجزاء للمحافظة على السياق.
                start = Math.Max(
                    start + 1,
                    end - DefaultOverlap);
            }
        }

        return chunks;
    }

    private static string NormalizeText(string text)
    {
        return string.Join(
            ' ',
            text.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries));
    }
}
