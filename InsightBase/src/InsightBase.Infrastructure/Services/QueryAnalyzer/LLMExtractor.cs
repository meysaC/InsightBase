using System.Text.Json;
using InsightBase.Application.Interfaces;
using InsightBase.Application.Models;
using Microsoft.Extensions.Logging;

namespace InsightBase.Infrastructure.Services.QueryAnalyzer
{
    public class LLMExtractor : ILLMExtractor // LLM tabanlı semantik bilgi çıkarımı yapan servis.
                                                // intet analizi, kontekt anlama ve yapısal olmayan bilgileri çıkarır.
    {
        private readonly ILLMClient _llm;
        private readonly ILogger<LLMExtractor> _logger;
        public LLMExtractor(ILLMClient llm, ILogger<LLMExtractor> logger) => (_llm, _logger) = (llm, logger);
        public async Task<LLMExtractionResult> ExtractAsync(string query, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogWarning("LLMExtractor: Boş sorgu için bilgi çıkarımı yapılamaz.", nameof(query));
                return new LLMExtractionResult
                {
                    OriginalQuery = query,
                    ExtractionFailed = true, //sonuç döndürür ama başarısızlık işaretli
                    ErrorMessage = "LLMExtractor: Sorgu boş olamaz."
                };
            }

            try
            {
                var prompt = BuildExtractionPrompt();
                var llmResponse = await _llm.GenerateJsonResponseAsync(prompt, query, cancellationToken);
                return ParseLLMResponse(llmResponse, query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LLMExtractor: Sorgu için bilgi çıkarımı sırasında hata oluştu.", query);
                return new LLMExtractionResult
                {
                    OriginalQuery = query,
                    ExtractionFailed = true,
                    ErrorMessage = $"LLMExtractor ExtractAsync: {ex.Message}"
                };
            }
        }
        private static string BuildExtractionPrompt()
        {
           return @""; // txt promptu buraya ekle !!!!!!!!!!!!!!!!!!!!!
        }

        private LLMExtractionResult ParseLLMResponse(LLMJsonResponse llmResponse, string originalQuery)
        {
           try
           {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };

            var jsonDoc = JsonSerializer.Deserialize<JsonElement>(llmResponse.RawJson, options);

            var result = new LLMExtractionResult
            {
                OriginalQuery = originalQuery,
                RawJson = llmResponse.RawJson,
            };
            
            // intent alanını işle
            if (jsonDoc.TryGetProperty("intent", out var intentElement))
            {
                result.Intents = intentElement.EnumerateArray()
                                                .Select(e => e.GetString() ?? string.Empty)
                                                .Where(s => !string.IsNullOrEmpty(s))
                                                .ToList();
            }


            // legal_areas alanını işle
            if(jsonDoc.TryGetProperty("legal_areas", out var areasElement))
            {
                result.LegalAreas = areasElement.EnumerateArray()
                                             .Select(e => e.GetString() ?? string.Empty)
                                             .Where(s => !string.IsNullOrEmpty(s))
                                             .ToList();
            }            

            // entities alanını işle
            if(jsonDoc.TryGetProperty("entities", out var entititiesElement))
            {
                ParseEntities(entititiesElement, result);
            }

            // Query_type alanını işle
            if(jsonDoc.TryGetProperty("query_type", out var queryTypeElement))
            {
                // result.QueryType = queryTypeElement.GetString();
            }

            // boolean flags
            result.RequiresCaseLaw = GetBooleanProperty(jsonDoc, "requires_case_law");
            result.RequiresLegislation = GetBooleanProperty(jsonDoc, "requires_legislation");

            // confidence_score alanını işle
            if (jsonDoc.TryGetProperty("confidence_score", out var scoreElement))
            {
                result.ConfidenceScore = scoreElement.GetDouble();
            }
            return result;

           }
           catch (JsonException ex)
           {
                _logger.LogWarning(ex, "LLMExtractor: LLM yanıtı ayrıştırılırken JSON hatası oluştu. ", llmResponse.RawJson);

                return new LLMExtractionResult
                {
                    OriginalQuery = originalQuery,
                    RawJson = llmResponse.RawJson,
                    ExtractionFailed = true,
                    ErrorMessage = "LLMExtractor ParseLLMResponse: LLM yanıtı ayrıştırılırken JSON hatası oluştu."
                };
           }
        }


        private void ParseEntities(JsonElement entitiesElement, LLMExtractionResult result)
        {
            if (entitiesElement.TryGetProperty("law_references", out var lawRefsElement))
            {
                result.LawReferences = ParseStringArray(lawRefsElement);
            }

            if (entitiesElement.TryGetProperty("courts", out var courtsElement))
            {
                result.LawReferences = ParseStringArray(courtsElement);
            }

            if (entitiesElement.TryGetProperty("date_expressions", out var dateExprElement))
            {
                result.LawReferences = ParseStringArray(dateExprElement);
            }

            if (entitiesElement.TryGetProperty("legal_concepts", out var legalConceptsElement))
            {
                result.LawReferences = ParseStringArray(legalConceptsElement);
            }

            if (entitiesElement.TryGetProperty("parties", out var partiesElement))
            {
                result.LawReferences = ParseStringArray(partiesElement);
            }

            if (entitiesElement.TryGetProperty("keywords", out var keywordsElement))
            {
                result.LawReferences = ParseStringArray(keywordsElement);
            }
        }

        private static List<string> ParseStringArray(JsonElement element) // JSON array i string liste dönüştürür
        {
            return element.EnumerateArray()
                            .Select(e => e.GetString() ?? string.Empty)
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToList();
        }

        private static bool GetBooleanProperty(JsonElement element, string propertName) // boolean özelliği alır
        {
            var property = element.TryGetProperty(propertName, out var prop) && prop.GetBoolean();
            return property;
        }

    }
}