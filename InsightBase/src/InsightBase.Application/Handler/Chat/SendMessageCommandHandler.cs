using InsightBase.Application.Commands.Chat;
using InsightBase.Application.Interfaces;
using InsightBase.Application.Models;
using MediatR;

namespace InsightBase.Application.Handler.Chat
{
    public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, RAGResponse>
    {
        private readonly IRAGOrchestrator _rag;
        private readonly ICurrentUserService _currentUser;
        public SendMessageCommandHandler(IRAGOrchestrator rag, ICurrentUserService currentUser) => (_rag, _currentUser) = (rag, currentUser);
        public Task<RAGResponse> Handle(SendMessageCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.Id;
            var response = _rag.GenerateAnswerAsync(
                request.Query,
                userId
            );
            return response;
        }

    }
}