using ChatWithDocs.Application.Interfaces;
using MediatR;

namespace ChatWithDocs.Application.Conversations.Queries;

public sealed record GetConversationQuery(Guid Id) : IRequest<ConversationDetail?>;

public sealed class GetConversationQueryHandler(IConversationRepository conversationRepository)
    : IRequestHandler<GetConversationQuery, ConversationDetail?>
{
    public Task<ConversationDetail?> Handle(GetConversationQuery request, CancellationToken cancellationToken) =>
        conversationRepository.GetDetailByIdAsync(request.Id, cancellationToken);
}
