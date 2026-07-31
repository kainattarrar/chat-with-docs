using System.Text;
using System.Text.RegularExpressions;

namespace ChatWithDocs.Infrastructure.Documents;

public static partial class TextChunker
{
    public const int ChunkSize = 1500;
    public const int ChunkOverlap = 200;

    public static List<string> Split(string text)
    {
        var chunks = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
            return chunks;

        var current = new StringBuilder();

        foreach (var segment in SegmentText(text))
        {
            if (current.Length > 0 && current.Length + segment.Length + 1 > ChunkSize)
            {
                chunks.Add(current.ToString().Trim());
                current = new StringBuilder(TakeOverlap(chunks[^1]));
            }

            if (current.Length > 0)
                current.Append(' ');
            current.Append(segment);
        }

        if (current.Length > 0)
            chunks.Add(current.ToString().Trim());

        return chunks;
    }

    // Breaks text into pieces no larger than ChunkSize, preferring paragraph then
    // sentence boundaries, falling back to a hard wrap only as a last resort.
    private static IEnumerable<string> SegmentText(string text)
    {
        foreach (var paragraph in ParagraphSplitRegex().Split(text))
        {
            var trimmedParagraph = paragraph.Trim();
            if (trimmedParagraph.Length == 0)
                continue;

            if (trimmedParagraph.Length <= ChunkSize)
            {
                yield return trimmedParagraph;
                continue;
            }

            foreach (var sentence in SentenceSplitRegex().Split(trimmedParagraph))
            {
                var trimmedSentence = sentence.Trim();
                if (trimmedSentence.Length == 0)
                    continue;

                if (trimmedSentence.Length <= ChunkSize)
                {
                    yield return trimmedSentence;
                    continue;
                }

                for (var i = 0; i < trimmedSentence.Length; i += ChunkSize)
                    yield return trimmedSentence.Substring(i, Math.Min(ChunkSize, trimmedSentence.Length - i));
            }
        }
    }

    private static string TakeOverlap(string chunk)
    {
        if (chunk.Length <= ChunkOverlap)
            return chunk;

        var start = chunk.Length - ChunkOverlap;
        var spaceIndex = chunk.IndexOf(' ', start);
        return spaceIndex >= 0 ? chunk[(spaceIndex + 1)..] : chunk[start..];
    }

    [GeneratedRegex(@"\r?\n\s*\r?\n")]
    private static partial Regex ParagraphSplitRegex();

    [GeneratedRegex(@"(?<=[.!?])\s+")]
    private static partial Regex SentenceSplitRegex();
}
