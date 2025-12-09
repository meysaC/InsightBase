using InsightBase.Application.Models;

namespace InsightBase.Application.Interfaces
{
    public interface IPromptBuilder
    {
        string BuildPrompt(QueryContext queryContext, List<SearchResult> searchResults, string userQuery);
    }
}