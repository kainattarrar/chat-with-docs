using ChatWithDocs.Application.Interfaces;
using MediatR;

namespace ChatWithDocs.Application.Conversations.Queries;

public sealed record ListConversationsQuery : IRequest<List<ConversationSummary>>;

public sealed class ListConversationsQueryHandler(IConversationRepository conversationRepository)
    : IRequestHandler<ListConversationsQuery, List<ConversationSummary>>
{
    public Task<List<ConversationSummary>> Handle(ListConversationsQuery request, CancellationToken cancellationToken) =>
        conversationRepository.GetAllAsync(cancellationToken);
}
