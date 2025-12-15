using InsightBase.Application.Models;

namespace InsightBase.Application.Interfaces
{
    public interface IAnswerValidator
    {
        Task<ValidationResult> ValidateAsync(string llmAnswer, List<SearchResult> sources, QueryContext queryContext, CancellationToken cancellationToken = default);
    }
}