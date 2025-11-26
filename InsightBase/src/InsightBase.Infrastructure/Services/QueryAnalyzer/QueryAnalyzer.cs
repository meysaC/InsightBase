using InsightBase.Application.Interfaces;
using InsightBase.Application.Models;
using Microsoft.Extensions.Logging;

namespace InsightBase.Infrastructure.Services.QueryAnalyzer
{
    public class QueryAnalyzer : IQueryAnalyzer // ana wuery analiz
                                                // regex ve llm organize eder /birleştirir
                                                // hybrid: determenistik + semantik analiz
    {
        private readonly RegexExtractor _regexExtractor;
        private readonly ILLMExtractor _llmExtractor;
        private readonly ILogger<QueryAnalyzer> _logger;
        public QueryAnalyzer(RegexExtractor regexExtractor, ILLMExtractor llmExtractor, ILogger<QueryAnalyzer> logger)
        {
            _regexExtractor = regexExtractor ?? throw new ArgumentNullException(nameof(regexExtractor));
            _llmExtractor = llmExtractor ?? throw new ArgumentNullException(nameof(llmExtractor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // query i analiz edip zenginleştirilmiş QueryContext dönüştürür
        public async Task<QueryContext> AnalyzeAsync(string query, string? userId = null, CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("Query boş kalamaz.", nameof(query));
            }

            _logger.LogInformation("Analiz edilen query: {Query}", query);

            var context = new QueryContext
            {
                OriginalQuery = query,
                UserId = userId ?? "anonymous",
                QueryTimeStamp = DateTime.UtcNow
            };

            // 1. Regex extraction (senkron, hızlı, deterministik)
            var regexResult = ExtractWithRegex(query, context);

            // 2. LLM extraction (asenkron, semantik, context-aware)
            var llmResult = await ExtractWithLLMAsync(query, cancellationToken);

            // 3. sonuçları birleştir
            MergeResults(context, regexResult, llmResult);

            PostProcessContext(context);

            _logger.LogInformation(
                "Query analizi tamamlandı. Intents: {Intents}, Areas: {Areas}, Confidence: {Confidence}",
                string.Join(", ", context.Intents),
                string.Join(", ", context.LegalAreas),
                context.ConfidenceScore);
            
            return context;
        }


        private RegexExtractionResult ExtractWithRegex(string query, QueryContext context) // determenistik analiz
        {
            try
            {
                var result = _regexExtractor.Extract(query);
                context.Source.RegexUsed = true;
                return result;
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning(ex, "QueryAnalyzer ExtractWithRegex: Regex analizi başarısız.");
                context.Source.Warnings.Add("Regex analizi başarısız.");
                return new RegexExtractionResult { OriginalQuery = query };
            }
        }
        private async Task<LLMExtractionResult> ExtractWithLLMAsync(string query, CancellationToken cancellationToken) // semantik analiz
        {
            try
            {
                var result = await _llmExtractor.ExtractAsync(query, cancellationToken);
                return result;
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning(ex, "QueryAnalyzer ExtractWithLLMAsync: LLM analizi başarısız.");
                return new LLMExtractionResult
                {
                    OriginalQuery = query,
                    ExtractionFailed = true,
                    ErrorMessage = ex.Message
                };
            }
        }    
        private void MergeResults(QueryContext context, RegexExtractionResult regexResult, LLMExtractionResult llmResult) // Regex öncelikli, LLM ile zenginleştir
        {
            // Law References - Regex öncelikli
            context.LawReferences = MergeLists(
                regexResult.LawReferences,
                llmResult.LawReferences,
                preferFirst: true);

            // Courts - Regex öncelikli
            context.Courts = MergeLists(
                regexResult.Courts,
                llmResult.Courts,
                preferFirst: true);

            // File Numbers - sadece regex'ten
            context.FileNumbers = regexResult.FileNumbers;

            // Dates - Regex öncelikli (daha güvenilir)
            context.StartDate = regexResult.StartDate ?? context.StartDate;
            context.EndDate = regexResult.EndDate ?? context.EndDate;
            context.DateExpressions = llmResult.DateExpressions;

            // Legal Areas - LLM'den al (daha iyi semantic anlama)
            context.LegalAreas = llmResult.LegalAreas.Any()
                ? llmResult.LegalAreas
                : regexResult.LegalAreas;

            // Intents - sadece LLM'den
            context.Intents = llmResult.Intents;

            // Semantik bilgiler - sadece LLM'den
            context.LegalConcepts = llmResult.LegalConcepts;
            context.Parties = llmResult.Parties;
            context.Keywords = llmResult.Keywords;

            // Meta bilgiler
            context.QueryType = llmResult.QueryType; // ???????????????
            context.ConfidenceScore = llmResult.ConfidenceScore;
            context.RequiresCaseLaw = llmResult.RequiresCaseLaw;
            context.RequiresLegislation = llmResult.RequiresLegislation;

            // Source tracking
            context.Source.LLMUsed = !llmResult.ExtractionFailed;
            context.Source.LLMRawJson = llmResult.RawJson;

            if (llmResult.ExtractionFailed)
            {
                context.Source.Warnings.Add($"LLM extraction failed: {llmResult.ErrorMessage}");
            }
        }
        private List<string> MergeLists(List<string> lawRefs1, List<string> lawRefs2, bool preferFirst = true)
        {
            if(preferFirst && lawRefs1.Any())
            {
                var result = new List<string>(lawRefs1);
                result.AddRange(lawRefs2.Where(s => !result.Contains(s, StringComparer.OrdinalIgnoreCase)));
                return result;
            }
            return lawRefs1.Concat(lawRefs2)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
        }
        private void PostProcessContext(QueryContext context) // contexti son process den geçir
        {
            // intent yoksa default veriliyor
            if(!context.Intents.Any())
            {
                context.Intents.Add("general_legal_question");
                context.Source.Warnings.Add("Niyet belirlenemedi. Genel hukuk sorusu olarak işaretlendi.");
            }

            // legal area yoksa query den tahmine et
            if(!context.LegalAreas.Any())
            {
                context.LegalAreas = DetectLegalAreasFromContext(context);
            }
            
            // search strategy flag ayarlanıyor
            DetermineSearchStrategy(context);
           
            // confidence score ayarlanıyor
            AdjustConfidenceScore(context);
            
            // tarih kontrol ediliyor
            ValidateDates(context);
        }


       
        private List<string> DetectLegalAreasFromContext(QueryContext context) // context den legal area belirleniyor 
        {
            var areas = new List<string>();

            foreach (var lawRefs in context.LawReferences)
            {
                if(lawRefs.StartsWith("TCK", StringComparison.OrdinalIgnoreCase))
                    areas.Add("ceza_hukuku");
                else if(lawRefs.StartsWith("TBK", StringComparison.OrdinalIgnoreCase))
                    areas.Add("borclar_hukuku");
                else if(lawRefs.StartsWith("TTK", StringComparison.OrdinalIgnoreCase))
                    areas.Add("ticaret_hukuku");
                else if(lawRefs.StartsWith("TMK", StringComparison.OrdinalIgnoreCase))
                    areas.Add("medeni_hukuku");
                else if(lawRefs.StartsWith("İş Kanunu", StringComparison.OrdinalIgnoreCase))
                    areas.Add("cis_hukuku");
            }
            return areas.Distinct().ToList();
        }
        private void DetermineSearchStrategy(QueryContext context)
        {
            // exact match gerekli mi
            context.RequiresExactMatch = context.LawReferences.Any() || context.FileNumbers.Any();

            //semantik search her zaman aktif ama bazen daha önemli
            context.RequiresSemanticSearch = true;

            // Intent e göre strateji ayarla 
            if(context.Intents.Contains("case_search") || context.Intents.Contains("precedent_search"))
            {
                context.RequiresCaseLaw = true;
            }

            if(context.Intents.Contains("article_explanation") || context.Intents.Contains("law_summary"))
            {
                context.RequiresLegislation = true;
            }
        }
        private void AdjustConfidenceScore(QueryContext context)
        {
            // eğer regex den kesin bilgi geldiyse confidence arttır
            if (context.LawReferences.Any() || context.Courts.Any())
            {
                context.ConfidenceScore = Math.Max(context.ConfidenceScore + 0.1, 1.0);
            }

            // llm başarısız olduysa confidence düşür
            if (!context.Source.LLMUsed)
            {
                context.ConfidenceScore = Math.Max(context.ConfidenceScore - 0.2, 0.0);
            }
        }
        private void ValidateDates(QueryContext context)
        {
           if(context.StartDate.HasValue && context.EndDate.HasValue)
            {
                // start date end date den sonra olamaz
                if(context.StartDate > context.EndDate)
                {
                    (context.StartDate, context.EndDate) = (context.EndDate, context.StartDate);
                    context.Source.Warnings.Add("Başlangıç ve bitiş taraihleri yer değiştirildi.");

                }

                // gelecek tarihler olamaz
                if(context.EndDate > DateTime.UtcNow)
                {
                    context.EndDate = DateTime.UtcNow;
                    context.Source.Warnings.Add("Bitiş tarihi, güncel tarih ile değiştirildi.");
                }
            }
        }

    }
}