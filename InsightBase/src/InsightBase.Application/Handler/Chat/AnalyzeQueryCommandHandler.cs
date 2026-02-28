using InsightBase.Application.Commands.Chat;
using InsightBase.Application.Interfaces;
using InsightBase.Application.Models;
using MediatR;

namespace InsightBase.Application.Handler.Chat
{
    public class AnalyzeQueryCommandHandler : IRequestHandler<AnalyzeQueryCommand, QueryContext>
    {
        private readonly IQueryAnalyzer _queryAnalyzer;
        private readonly ICurrentUserService _currentUserService;
        public AnalyzeQueryCommandHandler(IQueryAnalyzer queryAnalyzer, ICurrentUserService currentUserService) => (_queryAnalyzer, _currentUserService) = (queryAnalyzer, currentUserService);
        public async Task<QueryContext> Handle(AnalyzeQueryCommand request, CancellationToken cancellationToken)
        {
           var response = await _queryAnalyzer.AnalyzeAsync(
                request.Query,
                _currentUserService.Id,
                cancellationToken
           );
           return response;
        }

    }
}