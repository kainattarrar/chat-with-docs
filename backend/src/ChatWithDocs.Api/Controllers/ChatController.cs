using System.Text.Json;
using ChatWithDocs.Application.Chat;
using ChatWithDocs.Application.Chat.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ChatWithDocs.Api.Controllers;

public record AskQuestionRequest(string Question);

[ApiController]
[Route("api/chat")]
public class ChatController(IMediator mediator, ILogger<ChatController> logger) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpPost]
    public async Task Ask([FromBody] AskQuestionRequest? request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Question))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { error = "\"question\" is required." }, cancellationToken);
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";

        try
        {
            await foreach (var streamEvent in mediator.CreateStream(new AskQuestionQuery(request.Question), cancellationToken))
            {
                switch (streamEvent)
                {
                    case SourcesStreamEvent sources:
                        await WriteEventAsync("sources", sources.Sources, cancellationToken);
                        break;
                    case TokenStreamEvent token:
                        await WriteEventAsync("token", new { text = token.Text }, cancellationToken);
                        break;
                    case DoneStreamEvent:
                        await WriteEventAsync("done", new { }, cancellationToken);
                        break;
                }
            }
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
                await WriteEventAsync("error", new { message = "Something went wrong while generating the answer." }, cancellationToken);
            }
            catch
            {
                // The response stream may already be broken; nothing more we can do.
            }
        }
    }

    private async Task WriteEventAsync(string eventName, object data, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        await Response.WriteAsync($"event: {eventName}\ndata: {json}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
