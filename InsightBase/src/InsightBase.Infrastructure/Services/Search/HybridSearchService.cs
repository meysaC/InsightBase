using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Office.Word;
using InsightBase.Application.Interfaces;
using InsightBase.Application.Models;
using InsightBase.Infrastructure.Persistence.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace InsightBase.Infrastructure.Services.Search
{
    public class HybridSearchService : IHybridSearchService
    {
        private readonly IVectorSearchService _vectorSearch;
        private readonly IKeywordSearchService _keywordSearch;
        private readonly IMetadataFilterService _metadataFilter;
        private readonly IFusionRanker _fusionRanker;
        private readonly ILogger<HybridSearchService> _logger;


        // private readonly HybridSearchConfig _config; // !!!!!! appsettings.json a koy !!!! şu an class olarak !!!!!!
        private readonly IConfiguration _config;


        public HybridSearchService(
            IVectorSearchService vectorSearch,
            IKeywordSearchService keywordSearch,
            IMetadataFilterService metadataFilter,
            IFusionRanker fusionRanker,
            ILogger<HybridSearchService> logger,
            IConfiguration config) //
        {
            _vectorSearch = vectorSearch;
            _keywordSearch = keywordSearch;
            _metadataFilter = metadataFilter;
            _fusionRanker = fusionRanker;
            _logger = logger;
            _config = config;
        }
        public async Task<List<SearchResult>> SearchAsync(QueryContext queryContext, string? userId, CancellationToken cancellationToken = default)
        {
           _logger.LogInformation("Hybrid arama başlatılıyor: {Query}, User: {UserId}", queryContext, userId);

            // 1. acces control domaini oluşturuluyor
           var accessDomain = await BuildAccessDomain(userId);

           // 2. paralel arama yapılıyor (performans için)
           var searchTask = new List<Task<List<SearchResult>>>();

           // vector search (semantic)
           if(queryContext.RequiresSemanticSearch)
            {
                searchTask.Add(ExecuteVectorSearch(queryContext, accessDomain, cancellationToken));
            }

            // keyword search (BM25)
            if(ShouldUseKeywordSearch(queryContext))
            {
                searchTask.Add(ExecuteKeywordSearch(queryContext, accessDomain, cancellationToken));
            }

            // exact match search (dosya numarası, kanun maddesi...)
            if(queryContext.RequiresExactMatch)
            {
                searchTask.Add(ExecuteExactMatchSearch(queryContext, accessDomain, cancellationToken));
            }

            var SearchResultSets = await Task.WhenAll(searchTask);

            // 3. metadata filtering uygula
            var allResults = SearchResultSets.SelectMany(r => r).ToList();
            var filteredResults = await _metadataFilter.FilterAsync(
                                                        allResults,
                                                        queryContext,
                                                        cancellationToken
            );

            // 4. fusion ranking  ile birleştir ve sırala
            var rankedResults = await _fusionRanker.RankAsync(
                                                        filteredResults,
                                                        queryContext,
                                                        cancellationToken
            );

            // 5. top k sonucu döndür
            var finalResults = rankedResults
                                .Take(_config.GetValue<int>("RAG:HybridSearch:MaxResults"))
                                .ToList();

            _logger.LogInformation("HybridSearchService Hybrid search tamamnlandı. {Count} bulundu, ortalama sonuç skoru: {AvgScore:F3}", finalResults.Count, finalResults.Any() ? finalResults.Average(r => r.FinalScore) : 0);

            return null;// finalResults;
        }

        // vektör arama pgvector HNSW
        private async Task<List<SearchResult>> ExecuteVectorSearch(QueryContext queryContext, AccessDomain accessDomain, CancellationToken cancellationToken)
        {
           try
           {
                var results = await _vectorSearch.SearchAsync(queryContext.OriginalQuery, accessDomain, topK: _config.GetValue<int>("RAG:HybridSearch:VectorSearchTopK"), cancellationToken);

                foreach (var result in results)
                {
                    result.VectorScore = NormalizeScore(result.VectorScore, ScoreType.Cosine);
                }

                _logger.LogDebug("HybridSearchService Vector arama {Count} sonuç döndürdü", results.Count);
                return results;
           }
           catch (System.Exception ex)
           {
                _logger.LogError("HybridSearchService Vector arama başarısız", ex);
                return new List<SearchResult>();
           }
        }
        private async Task<List<SearchResult>> ExecuteKeywordSearch(QueryContext queryContext, AccessDomain accessDomain, CancellationToken cancellationToken)
        {
           try
           {
                // hukuki terimleri extract et
                var legalTerms = ExtractLegalTerms(queryContext);

                var results = await _keywordSearch.SearchAsync(legalTerms, accessDomain, topK: _config.GetValue<int>("RAG:HybridSearch:KeywordSearchTopK"), cancellationToken); 

                foreach (var result in results)
                {
                    result.BM25Score = NormalizeScore(result.BM25Score, ScoreType.BM25);
                }

                _logger.LogDebug("HybridSearchService Keyword arama {Count} sonuç döndürdü", results.Count);
                return results;
           }
           catch (System.Exception ex)
           {
                _logger.LogError("HybridSearchService Keyword arama başarısız", ex);
                return new List<SearchResult>();
           }
        }
        // exaxt match araması (kanun maddesi, dosya no...)
        private async Task<List<SearchResult>> ExecuteExactMatchSearch(QueryContext queryContext, AccessDomain accessDomain, CancellationToken cancellationToken)
        {
           try
           {
                var results = new List<SearchResult>();

                // kanun maddesi araması
                if(queryContext.LawReferences.Any())
                {
                    var lawResults = await _keywordSearch.ExactMatchLawReferencesAsync(queryContext.LawReferences, accessDomain, cancellationToken);
                    results.AddRange(lawResults);
                }

                // dosya numarası araması
                if(queryContext.FileNumbers.Any())
                {
                    var fileResults = await _keywordSearch.ExactMatchFileNumbersAsync(queryContext.FileNumbers, accessDomain, cancellationToken);
                    results.AddRange(fileResults);
                }

                // exact match e göre score veriliyor
                foreach (var result in results)
                {
                    result.ExactMatchScore = 1.0;
                }

                _logger.LogDebug("HybridSearchService tam eşleşme araması {Count} sonuç döndürdü", results.Count);
                return results;
           }
           catch (System.Exception ex)
           {
                _logger.LogError("HybridSearchService tam eşleşme araması başarısız", ex);
                return new List<SearchResult>();
           }
        }
    
    

        private async Task<AccessDomain> BuildAccessDomain(string userId)
        {
            // global mevzuat + kullanıcı firma dökümanları
            var domain = new AccessDomain
            {
                UserId = userId,
                IncludeGlobalData = true, //tüm kullanıcılar mevzuata erişebilir
                // UseIOrganizationIds = await GetUserOrganizationIds(userId),
                UserRoles = await GetUserRoles(userId)
            };
            return domain;
        }
        private bool ShouldUseKeywordSearch(QueryContext queryContext)
        {
            // spesifik hukuki terimler varsa keyword search şart
            return queryContext.LegalConcepts.Any() ||
                   queryContext.LawReferences.Any() ||
                   queryContext.Keywords.Any() ||
                   ContainsSpecificLegalTerms(queryContext.OriginalQuery);
        }
        private bool ContainsSpecificLegalTerms(string originalQuery)
        {
            // Daha genişletilebilir çözüm ??????????????!!!!!!!!!!!!!!!
            var legalTerms = new[]
            {
                "itirazın iptali", "menfi tespit", "alacağın tahsili",
                "tazminat", "şikayet", "istinaf", "temyiz", "karar düzeltme",
                "zamanaşımı", "ön ödeme", "haciz", "iflas", "konkordato"
            };

            return legalTerms.Any(term => 
                originalQuery.Contains(term, StringComparison.OrdinalIgnoreCase));
        }
        private List<string> ExtractLegalTerms(QueryContext queryContext)
        {
            var terms = new List<string>();

            // kewords
            terms.AddRange(queryContext.Keywords);

            // legal concepts
            terms.AddRange(queryContext.LegalConcepts);

            // law referances (text form)
            terms.AddRange(queryContext.LawReferences);

            // court names
            terms.AddRange(queryContext.Courts);

            return terms.Distinct().ToList();

        }
        private double NormalizeScore(double score, ScoreType type)
        {
            return type switch
            {
                ScoreType.Cosine => Math.Max(0, Math.Min(1, score)), // zaten 0-1 arası
                ScoreType.BM25 => Math.Max(0, Math.Min(1, score / 100.0 )), // genelde 0-100
                ScoreType.Euclidean => 1.0 / (1.0 + score), // distance -> benzerlik
                _ => score
            };
        }
    
    
        // !!!!!!!!!!!!!!!!!!!!!!!!!! Mock methods (gerçek implementasyonda db den gelir) !!!!!!!!!!!!!!!!!!!
        private Task<List<string>> GetUserOrganizationIds(string userId) 
            => Task.FromResult(new List<string> { "org_123" });

        private Task<List<string>> GetUserRoles(string userId) 
            => Task.FromResult(new List<string> { "user" });
    }
}