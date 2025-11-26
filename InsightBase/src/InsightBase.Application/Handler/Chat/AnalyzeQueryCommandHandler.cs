using InsightBase.Application.Commands.Chat;
using InsightBase.Application.Interfaces;
using InsightBase.Application.Models;
using MediatR;

namespace InsightBase.Application.Handler.Chat
{
    public class AnalyzeQueryCommandHandler : IRequestHandler<AnalyzeQueryCommand, QueryContext>
    {
        private readonly IQueryAnalyzer _queryAnalyzer;
        public AnalyzeQueryCommandHandler(IQueryAnalyzer queryAnalyzer) => _queryAnalyzer =  queryAnalyzer;
        public async Task<QueryContext> Handle(AnalyzeQueryCommand request, CancellationToken cancellationToken)
        {
           var response = await _queryAnalyzer.AnalyzeAsync(
                request.Query,
                request.UserId,
                cancellationToken
           );
           return response;
        }

    }
}