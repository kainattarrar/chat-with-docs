using System.Text;
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
            var chunks = await RetrieveTopChunksAsync(db, embeddingClient, request.Question, cancellationToken);

            var sources = chunks
                .Select(c => new
                {
                    documentId = c.DocumentId,
                    fileName = c.FileName,
                    chunkIndex = c.ChunkIndex,
                    snippet = Truncate(c.Content, SnippetLength),
                })
                .ToList();

            await WriteEventAsync(context.Response, "sources", sources, cancellationToken);

            if (chunks.Count == 0)
            {
                await WriteEventAsync(context.Response, "token", new { text = NoDocumentsMessage }, cancellationToken);
                await WriteEventAsync(context.Response, "done", new { }, cancellationToken);
                return;
            }

            var model = configuration["Claude:Model"] ?? DefaultModel;
            var parameters = new MessageCreateParams
            {
                MaxTokens = MaxTokens,
                System = SystemPrompt,
                Messages = [new() { Role = Role.User, Content = BuildUserContent(chunks, request.Question) }],
                Model = model,
            };

            await foreach (var rawEvent in anthropicClient.Messages.CreateStreaming(parameters, cancellationToken))
            {
                if (rawEvent.TryPickContentBlockDelta(out var delta) && delta.Delta.TryPickText(out var text))
                {
                    await WriteEventAsync(context.Response, "token", new { text = text.Text }, cancellationToken);
                }
            }

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
        var hasAnyChunks = await db.Chunks.AnyAsync(cancellationToken);
        if (!hasAnyChunks)
            return [];

        var queryEmbedding = await embeddingClient.EmbedQueryAsync(question, cancellationToken);
        var queryVector = new Vector(queryEmbedding);

        return await db.Chunks
            .OrderBy(c => c.Embedding!.CosineDistance(queryVector))
            .Take(TopK)
            .Select(c => new ChunkSearchResult(c.DocumentId, c.Document.FileName, c.ChunkIndex, c.Content))
            .ToListAsync(cancellationToken);
    }

    private static string BuildUserContent(IReadOnlyList<ChunkSearchResult> chunks, string question)
    {
        var sb = new StringBuilder();

        for (var i = 0; i < chunks.Count; i++)
        {
            sb.AppendLine($"[{i + 1}] (from \"{chunks[i].FileName}\"):");
            sb.AppendLine(chunks[i].Content);
            sb.AppendLine();
        }

        sb.Append("Question: ").Append(question);

        return sb.ToString();
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
