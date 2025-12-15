using InsightBase.Application.Models;

namespace InsightBase.Application.Interfaces
{
    public interface ICitationMapper
    {
        CitationMappingResult MapCitations(string llmAnswer, List<SearchResult> sources);
        string BuildCitationSummary(List<CitationMapping> citations);
    }
}