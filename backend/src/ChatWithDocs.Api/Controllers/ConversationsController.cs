using ChatWithDocs.Application.Conversations.Commands;
using ChatWithDocs.Application.Conversations.Queries;
using ChatWithDocs.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ChatWithDocs.Api.Controllers;

[ApiController]
[Route("api/conversations")]
public class ConversationsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ConversationSummary>>> List(CancellationToken cancellationToken)
    {
        var conversations = await mediator.Send(new ListConversationsQuery(), cancellationToken);
        return Ok(conversations);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ConversationDetail>> Get(Guid id, CancellationToken cancellationToken)
    {
        var conversation = await mediator.Send(new GetConversationQuery(id), cancellationToken);
        return conversation is null ? NotFound() : Ok(conversation);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await mediator.Send(new DeleteConversationCommand(id), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
