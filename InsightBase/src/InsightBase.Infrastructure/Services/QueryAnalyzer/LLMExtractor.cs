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
        private static string BuildExtractionPrompt() // static
        {
            return @"
                Sen bir Türk hukuku alanında uzmanlaşmış sorgu analiz asistanısın.
                Kullanıcının hukuki sorgusunu analiz edip aşağıdaki JSON formatında yapılandırılmış bilgi çıkar.

                ÇIKTI FORMATI (sadece JSON döndür, açıklama ekleme):
                {
                ""intent"": [""case_search"" , ""law_summary"" , ""article_explanation"" , ""comparison"" , ""general_legal_question"" , ""precedent_search""],
                ""legal_areas"": [""ceza_hukuku"", ""medeni_hukuk"", ""ticaret_hukuku"", ""borclar_hukuku"", ""is_hukuku"", ""idare_hukuku"", ""anayasa_hukuku""],
                ""entities"": {
                    ""law_references"": [""TCK 86"", ""TBK 49/1""],
                    ""courts"": [""Yargıtay 12. Ceza Dairesi""],
                    ""date_expressions"": [""son 5 yıl"", ""2020-2023 arası""],
                    ""legal_concepts"": [""tazminat"", ""haksız fiil"", ""zamanaşımı""],
                    ""parties"": [""kiracı"", ""kiraya veren"", ""şirket""],
                    ""keywords"": []
                },
                ""query_type"": ""complex"" , ""simple"" , ""multi_part"",
                ""requires_case_law"": true , false,
                ""requires_legislation"": true , false,
                ""confidence_score"": 0.0-1.0
                }

                ÖNEMLİ KURALLAR:
                1. Tarih ifadelerini aynen çıkar (""son 5 yıl"", ""2020 sonrası"")
                2. Kanun referanslarını normalize et (TCK md.86 → TCK 86)
                3. Mahkeme isimlerini tam olarak çıkar
                4. Intent'i doğru belirle (birden fazla olabilir)
                5. Legal area'yı mutlaka belirle
                6. Confidence score'u sorgunun netliğine göre ver
                7. Belirsiz olan alanları boş bırak, uydurma

                ÖRNEK 1:
                Input: ""Son 5 yıldaki Yargıtay 12. Ceza Dairesi'nin TCK 86 kapsamında verdiği kararları getir""
                Output:
                {
                ""intent"": [""case_search"", ""precedent_search""],
                ""legal_areas"": [""ceza_hukuku""],
                ""entities"": {
                    ""law_references"": [""TCK 86""],
                    ""courts"": [""Yargıtay 12. Ceza Dairesi""],
                    ""date_expressions"": [""son 5 yıl""],
                    ""legal_concepts"": [],
                    ""parties"": [],
                    ""keywords"": [""karar"", ""içtihat""]
                },
                ""query_type"": ""complex"",
                ""requires_case_law"": true,
                ""requires_legislation"": false,
                ""confidence_score"": 0.95
                }

                ÖRNEK 2:
                Input: ""TBK 49'daki haksız fiil düzenlemesi nedir?""
                Output:
                {
                ""intent"": [""article_explanation"", ""law_summary""],
                ""legal_areas"": [""borclar_hukuku""],
                ""entities"": {
                    ""law_references"": [""TBK 49""],
                    ""courts"": [],
                    ""date_expressions"": [],
                    ""legal_concepts"": [""haksız fiil""],
                    ""parties"": [],
                    ""keywords"": [""düzenleme"", ""açıklama""]
                },
                ""query_type"": ""simple"",
                ""requires_case_law"": false,
                ""requires_legislation"": true,
                ""confidence_score"": 0.90
                }

                Şimdi aşağıdaki sorguyu analiz et:
                
            ";
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