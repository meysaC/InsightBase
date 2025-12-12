using InsightBase.Application.Interfaces;
using InsightBase.Application.Models;
using InsightBase.Application.Models.Enum;
using InsightBase.Infrastructure.Persistence.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InsightBase.Infrastructure.Services.Search
{
    public class MetadataFilterService : IMetadataFilterService
    {
        private readonly ILogger<MetadataFilterService> _logger;
        private IConfiguration _configuration;
        public MetadataFilterService(ILogger<MetadataFilterService> logger, IConfiguration configuration) => (_logger, _configuration) = (logger, configuration);

        public Task<List<SearchResult>> FilterAsync(List<SearchResult> results, QueryContext queryContext, CancellationToken cancellationToken = default)
        {
            if(!results.Any())
                return Task.FromResult(results);

            _logger.LogInformation("Applying metadata filters to {Count} search results.", results.Count);

            var filtered = results.AsEnumerable();

            // 1. legal area filter
            if(queryContext.LegalAreas.Any() && _configuration.GetValue<bool>("RAG:MetadataFilter:EnableLegalAreaFilter"))
            {
                filtered = ApplyLegalAreaFilter(filtered, queryContext.LegalAreas);
                _logger.LogInformation("Applied legal area filter. Remaining results: {Count}.", filtered.Count());
            }

            // 2. court filter
            if(queryContext.Courts.Any() && _configuration.GetValue<bool>("RAG:MetadataFilter:EnableCourtFilter"))
            {
                filtered = ApplyCourtFilter(filtered, queryContext.Courts);
                _logger.LogInformation("Applied court filter. Remaining results: {Count}.", filtered.Count());
            }

            // 3. date range filter
            if((queryContext.StartDate.HasValue || queryContext.EndDate.HasValue) && _configuration.GetValue<bool>("RAG:MetadataFilter:EnableDateRangeFilter"))
            {
                filtered = ApplyDateRangeFilter(filtered, queryContext.StartDate, queryContext.EndDate);
                _logger.LogInformation("Applied date range filter. Remaining results: {Count}.", filtered.Count());
            }

            // 4. law reference filter
            if(queryContext.LawReferences.Any() && _configuration.GetValue<bool>("RAG:MetadataFilter:EnableLawReferenceFilter"))
            {
                filtered = ApplyLawReferenceFilter(filtered, queryContext.LawReferences);
                _logger.LogInformation("Applied law reference filter. Remaining results: {Count}.", filtered.Count());
            }

            // 5. Document type filter
            if (queryContext.RequiresCaseLaw || queryContext.RequiresLegislation)
            {
                filtered = ApplyDocumentTypeFilter(filtered, queryContext.RequiresCaseLaw, queryContext.RequiresLegislation);
                _logger.LogInformation("Applied document type filter. Remaining results: {Count}.", filtered.Count());
            }

            // 6. File number filter
            if (queryContext.FileNumbers.Any() && _configuration.GetValue<bool>("RAG:MetadataFilter:EnableFileNumberFilter"))
            {
                filtered = ApplyFileNumberFilter(filtered, queryContext.FileNumbers);
                _logger.LogInformation("Applied file number filter. Remaining results: {Count}.", filtered.Count());
            }

            // 7. Amendment filter (güncel dokümanları önceliklendir)
            if (_configuration.GetValue<bool>("RAG:MetadataFilter:PrioritizeCurrentLegislation"))
            {
                filtered = PrioritizeCurrentLegislation(filtered);
                _logger.LogInformation("Applied amendment filter to prioritize current legislation. Remaining results: {Count}.", filtered.Count());
            }

            var resultList = filtered.ToList();
            _logger.LogInformation(
                "Filtered from {Original} to {Filtered} results",
                results.Count, resultList.Count);

            return Task.FromResult(resultList);            
        }

        private IEnumerable<SearchResult> ApplyLegalAreaFilter(IEnumerable<SearchResult> results, List<string> legalAreas)
        {
            var filtered = results.Where(r =>
                legalAreas.Any(area =>
                    r.LegalArea.Equals(area, StringComparison.OrdinalIgnoreCase)));

            _logger.LogDebug("Legal area filter applied. Areas: {Areas}", 
                string.Join(", ", legalAreas));

            return filtered;
        }


        private IEnumerable<SearchResult> ApplyCourtFilter(IEnumerable<SearchResult> results, List<string> courts) //fuuzy match
        {
            var filtered = results.Where(r =>
                !string.IsNullOrEmpty(r.Court) &&
                courts.Any(court =>
                    r.Court.Contains(court, StringComparison.OrdinalIgnoreCase) ||
                    court.Contains(r.Court, StringComparison.OrdinalIgnoreCase)));

            _logger.LogDebug("Court filter applied. Courts: {Courts}", 
                string.Join(", ", courts));

            return filtered;
        }


        private IEnumerable<SearchResult> ApplyDateRangeFilter(IEnumerable<SearchResult> results, DateTime? startDate, DateTime? endDate)
        {
           var filtered = results.Where(r =>
            {
                if (!r.PublishDate.HasValue)
                    return false;

                var publishDate = r.PublishDate.Value;

                if (startDate.HasValue && publishDate < startDate.Value)
                    return false;

                if (endDate.HasValue && publishDate > endDate.Value)
                    return false;

                return true;
            });

            _logger.LogDebug("Date range filter applied. Range: {Start} - {End}", 
                startDate, endDate);

            return filtered;
        }


        private IEnumerable<SearchResult> ApplyLawReferenceFilter(IEnumerable<SearchResult> results, List<string> lawReferences)
        {
            var filtered = results.Where(r =>
                r.LawReferences.Any(lr =>
                    lawReferences.Any(queryLr =>
                        lr.Contains(queryLr, StringComparison.OrdinalIgnoreCase) ||
                        queryLr.Contains(lr, StringComparison.OrdinalIgnoreCase))));

            _logger.LogDebug("Law reference filter applied. References: {Refs}", 
                string.Join(", ", lawReferences));

            return filtered;
        }


        private IEnumerable<SearchResult> ApplyDocumentTypeFilter(IEnumerable<SearchResult> results, bool requiresCaseLaw, bool requiresLegislation)
        {
            if (!requiresCaseLaw && !requiresLegislation)
                return results;

            var allowedTypes = new List<DocumentType>();

            if (requiresCaseLaw)
                allowedTypes.Add(DocumentType.CaseLaw);

            if (requiresLegislation)
            {
                allowedTypes.Add(DocumentType.Legislation);
                allowedTypes.Add(DocumentType.Regulation);
            }

            var filtered = results.Where(r => allowedTypes.Contains(r.DocumentType));

            _logger.LogDebug("Document type filter applied. Types: {Types}", 
                string.Join(", ", allowedTypes));

            return filtered;
        }


        private IEnumerable<SearchResult> ApplyFileNumberFilter(IEnumerable<SearchResult> results, List<string> fileNumbers)
        {
            var filtered = results.Where(r =>
                !string.IsNullOrEmpty(r.FileNumber) &&
                fileNumbers.Any(fn =>
                    r.FileNumber.Equals(fn, StringComparison.OrdinalIgnoreCase)));

            _logger.LogDebug("File number filter applied. Numbers: {Numbers}", 
                string.Join(", ", fileNumbers));

            return filtered;
        }


        private IEnumerable<SearchResult> PrioritizeCurrentLegislation(IEnumerable<SearchResult> results)
        {
           var sorted = results.OrderBy(r => r.IsAmended ? 1 : 0);

            _logger.LogDebug("Prioritized current legislation");

            return sorted;
        }



        public Task<List<SearchResult>> ApplyCustomFiltersAsync(List<SearchResult> results, Dictionary<string, object> customFilters, CancellationToken cancellationToken = default)
        {
            var filtered = results.AsEnumerable();

            foreach (var filter in customFilters)
            {
                filtered = filter.Key switch
                {
                    "min_score" => filtered.Where(r => r.FinalScore >= Convert.ToDouble(filter.Value)),
                    "max_age_years" => filtered.Where(r => 
                        r.PublishDate.HasValue && 
                        (DateTime.UtcNow - r.PublishDate.Value).TotalDays / 365 <= Convert.ToDouble(filter.Value)),
                    "exclude_amended" => filtered.Where(r => !r.IsAmended),
                    "organization_id" => filtered.Where(r => r.OrganizationId == filter.Value.ToString()),
                    _ => filtered
                };
            }

            return Task.FromResult(filtered.ToList());
        }
        public FilterStatistics GetFilterStatistics( List<SearchResult> results, QueryContext queryContext)
        {
            var stats = new FilterStatistics
            {
                TotalResults = results.Count,
                LegalAreaDistribution = results
                    .GroupBy(r => r.LegalArea)
                    .ToDictionary(g => g.Key, g => g.Count()),
                CourtDistribution = results
                    .Where(r => !string.IsNullOrEmpty(r.Court))
                    .GroupBy(r => r.Court!)
                    .ToDictionary(g => g.Key, g => g.Count()),
                DocumentTypeDistribution = results
                    .GroupBy(r => r.DocumentType)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                DateRangeDistribution = results
                    .Where(r => r.PublishDate.HasValue)
                    .GroupBy(r => r.PublishDate!.Value.Year)
                    .OrderByDescending(g => g.Key)
                    .Take(10)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                AmendedCount = results.Count(r => r.IsAmended),
                CurrentCount = results.Count(r => !r.IsAmended)
            };

            return stats;
        }
    }
}