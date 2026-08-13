using Anthropic;
using Anthropic.Models.Messages;
using ChatWithDocs.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChatWithDocs.Infrastructure.Chat;

public class AnthropicTitleGenerationService(
    AnthropicClient client,
    IConfiguration configuration,
    ILogger<AnthropicTitleGenerationService> logger) : ITitleGenerationService
{
    private const long MaxTokens = 30;
    private const string DefaultModel = "claude-haiku-4-5-20251001";
    private const int FallbackTitleMaxLength = 60;

    private const string SystemPrompt =
        "Generate a short title (3-6 words) summarizing the following question. " +
        "Return ONLY the title text - no quotes, no punctuation, no preamble.";

    public async Task<string> GenerateTitleAsync(string firstQuestion, CancellationToken cancellationToken)
    {
        try
        {
            var model = configuration["Claude:TitleModel"] ?? configuration["Claude:Model"] ?? DefaultModel;
            var parameters = new MessageCreateParams
            {
                MaxTokens = MaxTokens,
                System = SystemPrompt,
                Messages = [new() { Role = Role.User, Content = firstQuestion }],
                Model = model,
            };

            var message = await client.Messages.Create(parameters, cancellationToken);
            var title = ExtractText(message)?.Trim();

            return string.IsNullOrWhiteSpace(title) ? FallbackTitle(firstQuestion) : title;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate a conversation title; falling back to a truncated question.");
            return FallbackTitle(firstQuestion);
        }
    }

    private static string? ExtractText(Message message) =>
        message.Content
            .Select(block => block.TryPickText(out var text) ? text.Text : null)
            .FirstOrDefault(text => text is not null);

    private static string FallbackTitle(string question)
    {
        var trimmed = question.Trim();
        return trimmed.Length <= FallbackTitleMaxLength
            ? trimmed
            : trimmed[..FallbackTitleMaxLength].TrimEnd() + "…";
    }
}
