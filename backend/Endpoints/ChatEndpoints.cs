using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using backend.Data;
using backend.Services;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace backend.Endpoints;

public record ChatRequest(string Question);

public static class ChatEndpoints
{
    private const int TopK = 5;
    private const int SnippetLength = 300;
    private const long MaxTokens = 1024;
    private const string DefaultModel = "claude-haiku-4-5-20251001";

    private const string SystemPrompt =
        "Answer the user's question using ONLY the provided context passages. " +
        "If the answer isn't in the context, say it isn't found in the documents rather than guessing. " +
        "Be concise.";

    private const string NoDocumentsMessage =
        "No documents have been added yet, so there's nothing to search. Upload a document and ask again.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly object[] NoSources = [];

    public static void MapChatEndpoints(this WebApplication app)
    {
        app.MapPost("/api/chat", HandleChatAsync);
    }

    private static async Task HandleChatAsync(
        ChatRequest? request,
        HttpContext context,
        AppDbContext db,
        VoyageEmbeddingClient embeddingClient,
        AnthropicClient anthropicClient,
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Question))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "\"question\" is required." }, cancellationToken);
            return;
        }

        var logger = loggerFactory.CreateLogger("backend.Endpoints.ChatEndpoints");

        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";

        try
        {
            var hasAnyChunks = await db.Chunks.AnyAsync(cancellationToken);
            if (!hasAnyChunks)
            {
                await WriteEventAsync(context.Response, "sources", NoSources, cancellationToken);
                await WriteEventAsync(context.Response, "token", new { text = NoDocumentsMessage }, cancellationToken);
                await WriteEventAsync(context.Response, "done", new { }, cancellationToken);
                return;
            }

            var chunks = await RetrieveTopChunksAsync(db, embeddingClient, request.Question, cancellationToken);

            var model = configuration["Claude:Model"] ?? DefaultModel;
            var parameters = new MessageCreateParams
            {
                MaxTokens = MaxTokens,
                System = SystemPrompt,
                Messages = [new() { Role = Role.User, Content = BuildUserContent(chunks, request.Question) }],
                Model = model,
            };

            // Citations tell us which document blocks Claude actually grounded its answer in —
            // that, not raw top-K retrieval, is what determines what we surface as "sources".
            var citedDocumentIndices = new List<long>();
            var seenIndices = new HashSet<long>();

            await foreach (var rawEvent in anthropicClient.Messages.CreateStreaming(parameters, cancellationToken))
            {
                if (!rawEvent.TryPickContentBlockDelta(out var contentDelta))
                    continue;

                if (contentDelta.Delta.TryPickText(out var text))
                {
                    await WriteEventAsync(context.Response, "token", new { text = text.Text }, cancellationToken);
                }
                else if (contentDelta.Delta.TryPickCitations(out var citationsDelta)
                    && citationsDelta.Citation.DocumentIndex is long documentIndex
                    && seenIndices.Add(documentIndex))
                {
                    citedDocumentIndices.Add(documentIndex);
                }
            }

            var sources = citedDocumentIndices
                .Where(i => i >= 0 && i < chunks.Count)
                .Select(i => chunks[(int)i])
                .Select(c => new
                {
                    documentId = c.DocumentId,
                    fileName = c.FileName,
                    chunkIndex = c.ChunkIndex,
                    snippet = Truncate(c.Content, SnippetLength),
                })
                .ToList();

            await WriteEventAsync(context.Response, "sources", sources, cancellationToken);
            await WriteEventAsync(context.Response, "done", new { }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected; nothing more to do.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Chat request failed for question: {Question}", request.Question);

            try
            {
                await WriteEventAsync(context.Response, "error", new { message = "Something went wrong while generating the answer." }, cancellationToken);
            }
            catch
            {
                // The response stream may already be broken; nothing more we can do.
            }
        }
    }

    private static async Task<List<ChunkSearchResult>> RetrieveTopChunksAsync(
        AppDbContext db,
        VoyageEmbeddingClient embeddingClient,
        string question,
        CancellationToken cancellationToken)
    {
        var queryEmbedding = await embeddingClient.EmbedQueryAsync(question, cancellationToken);
        var queryVector = new Vector(queryEmbedding);

        return await db.Chunks
            .Select(c => new
            {
                c.DocumentId,
                FileName = c.Document.FileName,
                c.ChunkIndex,
                c.Content,
                Distance = c.Embedding!.CosineDistance(queryVector),
            })
            .OrderBy(x => x.Distance)
            .Take(TopK)
            .Select(x => new ChunkSearchResult(x.DocumentId, x.FileName, x.ChunkIndex, x.Content))
            .ToListAsync(cancellationToken);
    }

    // One document content block per retrieved chunk, citations enabled, so Claude's response
    // tags each grounded claim with the document_index it came from. document_index lines up
    // with this list's order since only document blocks (not the trailing text block) count.
    private static List<ContentBlockParam> BuildUserContent(IReadOnlyList<ChunkSearchResult> chunks, string question)
    {
        List<ContentBlockParam> content = [];

        foreach (var chunk in chunks)
        {
            content.Add(new DocumentBlockParam(new PlainTextSource(chunk.Content))
            {
                Title = chunk.FileName,
                Citations = new CitationsConfigParam { Enabled = true },
            });
        }

        content.Add(new TextBlockParam($"Question: {question}"));

        return content;
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength].TrimEnd() + "…";

    private static async Task WriteEventAsync(HttpResponse response, string eventName, object data, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        await response.WriteAsync($"event: {eventName}\ndata: {json}\n\n", cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }

    private record ChunkSearchResult(Guid DocumentId, string FileName, int ChunkIndex, string Content);
}
