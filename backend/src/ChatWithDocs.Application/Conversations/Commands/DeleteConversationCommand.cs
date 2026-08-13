using ChatWithDocs.Application.Interfaces;
using MediatR;

namespace ChatWithDocs.Application.Conversations.Commands;

public sealed record DeleteConversationCommand(Guid Id) : IRequest<bool>;

public sealed class DeleteConversationCommandHandler(
    IConversationRepository conversationRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteConversationCommand, bool>
{
    public async Task<bool> Handle(DeleteConversationCommand request, CancellationToken cancellationToken)
    {
        var conversation = await conversationRepository.GetByIdAsync(request.Id, cancellationToken);
        if (conversation is null)
            return false;

        await conversationRepository.RemoveAsync(conversation, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
