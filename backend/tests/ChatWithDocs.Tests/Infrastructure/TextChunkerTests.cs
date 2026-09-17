using ChatWithDocs.Infrastructure.Documents;

namespace ChatWithDocs.Tests.Infrastructure;

public class TextChunkerTests
{
    [Fact]
    public void Split_EmptyText_ReturnsNoChunks()
    {
        var chunks = TextChunker.Split(string.Empty);

        Assert.Empty(chunks);
    }

    [Fact]
    public void Split_WhitespaceOnlyText_ReturnsNoChunks()
    {
        var chunks = TextChunker.Split("   \n\n\t  \n  ");

        Assert.Empty(chunks);
    }

    [Fact]
    public void Split_NullText_ReturnsNoChunks()
    {
        var chunks = TextChunker.Split(null!);

        Assert.Empty(chunks);
    }

    [Fact]
    public void Split_TextShorterThanOneChunk_ReturnsSingleChunkWithFullContent()
    {
        const string text = "This is a short document with just one paragraph of content.";

        var chunks = TextChunker.Split(text);

        var chunk = Assert.Single(chunks);
        Assert.Equal(text, chunk);
    }

    [Fact]
    public void Split_TrimsLeadingAndTrailingWhitespaceFromChunks()
    {
        const string text = "   \n  Padded content on both sides.   \n\n  ";

        var chunks = TextChunker.Split(text);

        var chunk = Assert.Single(chunks);
        Assert.Equal("Padded content on both sides.", chunk);
    }

    [Fact]
    public void Split_MultipleShortParagraphs_MergesThemIntoOneChunkWhenTheyFit()
    {
        var text = "First short paragraph." + "\n\n" + "Second short paragraph." + "\n\n" + "Third short paragraph.";

        var chunks = TextChunker.Split(text);

        var chunk = Assert.Single(chunks);
        Assert.Contains("First short paragraph.", chunk);
        Assert.Contains("Second short paragraph.", chunk);
        Assert.Contains("Third short paragraph.", chunk);
    }

    [Fact]
    public void Split_AllChunks_NeverExceedConfiguredChunkSize()
    {
        var text = BuildManySentencesAsOneParagraph(sentenceCount: 400);

        var chunks = TextChunker.Split(text);

        Assert.All(chunks, chunk => Assert.True(
            chunk.Length <= TextChunker.ChunkSize,
            $"Chunk of length {chunk.Length} exceeds ChunkSize {TextChunker.ChunkSize}."));
    }

    [Fact]
    public void Split_LongSingleParagraphWithNoBreaks_FallsBackToSentenceSplittingAndProducesMultipleChunks()
    {
        // One paragraph (no blank lines) but long enough to need multiple chunks,
        // exercising the sentence-boundary fallback rather than the paragraph split.
        var text = BuildManySentencesAsOneParagraph(sentenceCount: 400);

        var chunks = TextChunker.Split(text);

        Assert.True(chunks.Count > 1, "Expected long text with no paragraph breaks to still be split into multiple chunks.");
    }

    [Fact]
    public void Split_TextLongerThanChunkSizeWithNoWhitespaceAtAll_FallsBackToHardWrapping()
    {
        // No spaces, no sentence punctuation anywhere - forces the last-resort hard wrap.
        var text = new string('a', TextChunker.ChunkSize * 2 + 137);

        var chunks = TextChunker.Split(text);

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, chunk => Assert.True(chunk.Length <= TextChunker.ChunkSize));
        // The very first chunk has nothing to flush before it, so it fills up exactly
        // to the configured chunk size from the hard-wrapped segment.
        Assert.Equal(TextChunker.ChunkSize, chunks[0].Length);
    }

    [Fact]
    public void Split_ConsecutiveChunks_OverlapAtAWordBoundaryFromThePreviousChunk()
    {
        var text = BuildManySentencesAsOneParagraph(sentenceCount: 400);

        var chunks = TextChunker.Split(text);

        Assert.True(chunks.Count > 1, "Test requires multiple chunks to verify overlap between them.");

        for (var i = 0; i < chunks.Count - 1; i++)
        {
            var expectedSeed = ExpectedOverlapSeed(chunks[i]);
            Assert.False(string.IsNullOrEmpty(expectedSeed));
            Assert.StartsWith(expectedSeed, chunks[i + 1]);
        }
    }

    [Fact]
    public void Split_ExcessBlankLinesBetweenParagraphs_IgnoresEmptyParagraphsInsteadOfEmittingBlankChunks()
    {
        var text = "First paragraph." + "\n\n\n\n\n" + "Second paragraph." + "\n\n   \n\n" + "Third paragraph.";

        var chunks = TextChunker.Split(text);

        var chunk = Assert.Single(chunks);
        Assert.DoesNotContain("  ", chunk);
        Assert.Contains("First paragraph.", chunk);
        Assert.Contains("Second paragraph.", chunk);
        Assert.Contains("Third paragraph.", chunk);
    }

    // Mirrors TextChunker's private TakeOverlap so the overlap test can assert against
    // the same word-boundary-snapping contract without re-implementing the chunker itself.
    private static string ExpectedOverlapSeed(string chunk)
    {
        if (chunk.Length <= TextChunker.ChunkOverlap)
            return chunk;

        var start = chunk.Length - TextChunker.ChunkOverlap;
        var spaceIndex = chunk.IndexOf(' ', start);
        return spaceIndex >= 0 ? chunk[(spaceIndex + 1)..] : chunk[start..];
    }

    private static string BuildManySentencesAsOneParagraph(int sentenceCount)
    {
        var sentences = Enumerable.Range(0, sentenceCount)
            .Select(i => $"Sentence number {i:0000} adds some unique filler content to pad it out nicely.");
        return string.Join(" ", sentences);
    }
}
