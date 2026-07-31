using System.Runtime.CompilerServices;
using Anthropic;
using Anthropic.Models.Messages;
using ChatWithDocs.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ChatWithDocs.Infrastructure.Chat;

public class AnthropicChatService(AnthropicClient client, IConfiguration configuration) : IChatService
{
    private const long MaxTokens = 1024;
    private const string DefaultModel = "claude-haiku-4-5-20251001";

    private const string SystemPrompt =
        "Answer the user's question using ONLY the provided context passages. " +
        "If the answer isn't in the context, say it isn't found in the documents rather than guessing. " +
        "Be concise.";

    public async IAsyncEnumerable<ChatCompletionChunk> StreamAnswerAsync(
        string question,
        IReadOnlyList<ChunkSearchResult> context,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var model = configuration["Claude:Model"] ?? DefaultModel;
        var parameters = new MessageCreateParams
        {
            MaxTokens = MaxTokens,
            System = SystemPrompt,
            Messages = [new() { Role = Role.User, Content = BuildUserContent(context, question) }],
            Model = model,
        };

        await foreach (var rawEvent in client.Messages.CreateStreaming(parameters, cancellationToken))
        {
            if (!rawEvent.TryPickContentBlockDelta(out var contentDelta))
                continue;

            if (contentDelta.Delta.TryPickText(out var text))
            {
                yield return new ChatTextChunk(text.Text);
            }
            else if (contentDelta.Delta.TryPickCitations(out var citationsDelta)
                && citationsDelta.Citation.DocumentIndex is long documentIndex)
            {
                yield return new ChatCitationChunk((int)documentIndex);
            }
        }
    }

    // One document content block per retrieved chunk, citations enabled, so Claude's response
    // tags each grounded claim with the document_index it came from. document_index lines up
    // with the context list's order since only document blocks (not the trailing text block) count.
    private static List<ContentBlockParam> BuildUserContent(IReadOnlyList<ChunkSearchResult> context, string question)
    {
        List<ContentBlockParam> content = [];

        foreach (var chunk in context)
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
}
