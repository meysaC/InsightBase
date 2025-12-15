using InsightBase.Application.Interfaces;
using InsightBase.Application.Models;
using InsightBase.Application.Models.Enum;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig.Fonts.Encodings;

namespace InsightBase.Infrastructure.Services.Search
{
    public class FusionRanker : IFusionRanker // vector + bm25 + metadata
                                            // reciprocal rank fusion ya da weighted scoring 
    {
        private readonly ILogger<FusionRanker> _logger;
        private readonly IConfiguration _configuration;
        public FusionRanker(ILogger<FusionRanker> logger, IConfiguration configuration) =>(_logger, _configuration) = (logger, configuration);
        public Task<List<SearchResult>> RankAsync(List<SearchResult> results, QueryContext queryContext, CancellationToken cancellationToken = default)
        {
            if(!results.Any()) return Task.FromResult(results);
            _logger.LogInformation("Starting fusion ranking for {Count} results.", results.Count);

            // 1. duplicate elimination
            var uniqueResults = EliminateDuplicates(results);

            // 2. score calculation
            foreach (var result in results)
            {
                CalculateFinalScore(result, queryContext);
            }

            // 3. metadata boost ayarlanıyor
            ApplyMetadataBoost(uniqueResults, queryContext);

            // source diversity
            var diversifiedResults = EnsureSourceDiversity(uniqueResults);

            // 5. prirority reranking (kanun > içtihat > yorum)
            var rerankedResults = ApplyPriorityReranking(diversifiedResults , queryContext);

            // 6. context grouping (aynı belgeye ait chunkların gruplanması)
            var groupedResults = ApplyContextGrouping(rerankedResults);

            // 7. final sorting
            var sortedResults = groupedResults
                                .OrderByDescending(r => r.FinalScore)
                                .ThenByDescending(r => r.Relevance)
                                .ToList();
            _logger.LogInformation("Fusion ranking completed. Returning {Count} ranked results. Top result score: {TopScore:F3}", sortedResults.Count, sortedResults.FirstOrDefault()?.FinalScore ?? 0);
            return Task.FromResult(sortedResults);
        }



        private List<SearchResult> EliminateDuplicates(List<SearchResult> results)
        {
            var seen = new HashSet<string>();
            var unique = new List<SearchResult>();

            foreach (var result in results)
            {
                // chunk id ile duplicate check
                if(seen.Add(result.ChunkId))
                {
                    unique.Add(result);
                }
                else
                {
                    // duplicate bylundu en yüksek skorlu olanı tut
                    var existing = unique.First(r => r.ChunkId == result.ChunkId);
                    if(result.VectorScore + result.BM25Score > existing.VectorScore + existing.BM25Score)
                    {
                        unique.Remove(existing);
                        unique.Add(result);
                    }
                }
            }
            _logger.LogInformation("Eliminated duplicates. Unique result-> {UniqueCount}", unique.Count);
            return unique;
        }
        private void CalculateFinalScore(SearchResult result, QueryContext queryContext)
        {
            // base formula: 0.55 * vector +0.35 * bm25 + 0.10 * metadata
            var baseScore = 
                (_configuration.GetValue<double>("RAG:FusionRanking:VectorWeight") * result.VectorScore) +
                (_configuration.GetValue<double>("RAG:FusionRanking:BM25Weight") * result.BM25Score) +
                (_configuration.GetValue<double>("RAG:FusionRanking:MetadataWeight") * result.MetadataScore);
            
            // exact match varsa extra boost
            if(result.ExactMatchScore > 0)
            {
                baseScore = (baseScore * 0.7) + (result.ExactMatchScore * 0.3);
            }

            // recancy boost (son 2 yıl için ekstra puan)
            if(result.PublishDate.HasValue)
            {
                var recencyBoost = CalculateRecencyBoost(result.PublishDate.Value);
                baseScore *= (1.0 + recencyBoost);
            }

            // query intente göre boost
            var intentBoost = CalculateIntentBoost(result, queryContext);
            baseScore *= (1.0 + intentBoost);

            result.FinalScore = Math.Max(0, Math.Min(1, baseScore)); // 0-1 arası normalize
        }
        private void ApplyMetadataBoost(List<SearchResult> results, QueryContext queryContext)
        {
            foreach (var result in results)
            {
                var boost = 0.0;

                // legal area match
                if (queryContext.LegalAreas.Any() && 
                    queryContext.LegalAreas.Contains(result.LegalArea, StringComparer.OrdinalIgnoreCase))
                {
                    boost += 0.1;
                }

                // court match
                if (queryContext.Courts.Any() && 
                    queryContext.Courts.Any(c => result.Court?.Contains(c, StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    boost += 0.15;
                }

                // date range match
                if (queryContext.StartDate.HasValue && 
                    result.PublishDate.HasValue &&
                    result.PublishDate >= queryContext.StartDate &&
                    result.PublishDate <= (queryContext.EndDate ?? DateTime.UtcNow))
                {
                    boost += 0.05;
                }

                // law reference match
                if (queryContext.LawReferences.Any() &&
                    result.LawReferences.Any(lr => queryContext.LawReferences.Contains(lr, StringComparer.OrdinalIgnoreCase)))
                {
                    boost += 0.2; // En yüksek boost
                }

                result.MetadataScore = boost;
                result.FinalScore *= (1.0 + boost);
            }
        }
        private List<SearchResult> EnsureSourceDiversity(List<SearchResult> results) // tek bir dökümandan en fazla 3 chunk
        {
            var diversified = new List<SearchResult>();
            var documentCounts = new Dictionary<string, int>();

            foreach (var result in results.OrderByDescending(r => r.FinalScore))
            {
                var docId = result.DocumentId;
                var currentCount = documentCounts.GetValueOrDefault(docId, 0);

                // döküman başına maksimum 3 chunk !!!!!!!!!!!!!!??????????????
                if(currentCount < _configuration.GetValue<int>("RAG:fusionRanking:MaxChunksPerDocument"))
                {
                    diversified.Add(result);
                    documentCounts[docId] = currentCount + 1;
                }
                else
                {
                    _logger.LogTrace("Skipping chunk {ChunkId} from document {DocumentId} to ensure source diversity.", result.ChunkId, docId);
                }
            }
            return diversified;
        }
        private List<SearchResult> ApplyPriorityReranking(List<SearchResult> results, QueryContext queryContext)
        {
            // eğer legislation gerekiyorsa kanunları yukarı çıkar
            if (queryContext.RequiresLegislation)
            {
                foreach (var result in results.Where(r => r.DocumentType == DocumentType.Legislation))
                {
                    result.FinalScore *= 1.15;
                }
            }

            // eğer case law gerekiyorsa içtihatları yukarı çıkar
            if (queryContext.RequiresCaseLaw)
            {
                foreach (var result in results.Where(r => r.DocumentType == DocumentType.CaseLaw))
                {
                    result.FinalScore *= 1.15;
                }
            }

            // genel sorularda commentaryleri aşağı çek
            if (queryContext.Intents.Contains("general_legal_question"))
            {
                foreach (var result in results.Where(r => r.DocumentType == DocumentType.Commentary))
                {
                    result.FinalScore *= 0.8;
                }
            }

            return results;
        }
        private List<SearchResult> ApplyContextGrouping(List<SearchResult> results)
        {
            if (!_configuration.GetValue<bool>("RAG:FusionRanking:EnableContextGrouping"))
            {
                return results;
            }

            var grouped = results
                .GroupBy(r => r.DocumentId)
                .SelectMany(g =>
                {
                    // her dokümandan maksimum 3 chunk al !!!!!!!!!!!!!!!!!!!!!!!????????
                    var topChunks = g.OrderByDescending(r => r.FinalScore)
                        .Take(_configuration.GetValue<int>("RAG:FusionRanking:MaxChunksPerDocument"))
                        .ToList();

                    // eğer aynı dokümandan birden fazla chunk varsa context birleştir
                    if (topChunks.Count > 1 && _configuration.GetValue<bool>("RAG:FusionRanking:MergeContiguousChunks"))
                    {
                        MergeContiguousChunks(topChunks);
                    }

                    return topChunks;
                })
                .ToList();

            return grouped;
        }



        private double CalculateRecencyBoost(DateTime publishDate) // son 2 yıl
        {
            var age = DateTime.UtcNow - publishDate;
            var ageInYears = age.TotalDays / 365.0;

            if (ageInYears <= 1)
                return 0.1; // %10 boost
            else if (ageInYears <= 2)
                return 0.05; // %5 boost
            else
                return 0.0; // Boost yok
        }
        private double CalculateIntentBoost(SearchResult result, QueryContext queryContext)
        {
            var boost = 0.0;

            // case search intent + case law document
            if (queryContext.Intents.Contains("case_search") && 
                result.DocumentType == DocumentType.CaseLaw)
            {
                boost += 0.1;
            }

            // article explanation + legislation
            if (queryContext.Intents.Contains("article_explanation") && 
                result.DocumentType == DocumentType.Legislation)
            {
                boost += 0.1;
            }

            return boost;
        }
        private void MergeContiguousChunks(List<SearchResult> chunks)
        {
            // chunk indexe göre sırala
            var sorted = chunks.OrderBy(c => c.ChunkIndex).ToList();

            for (int i = 0; i < sorted.Count - 1; i++)
            {
                var current = sorted[i];
                var next = sorted[i + 1];

                // eğer chunklar ardışıksa (index farkı 1)
                if (next.ChunkIndex - current.ChunkIndex == 1)
                {
                    current.IsMergedWithNext = true;
                    current.MergedContent = $"{current.Content}\n\n{next.Content}";
                }
            }
        }
    }
}