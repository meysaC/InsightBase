using System.Text.RegularExpressions;
using InsightBase.Application.Interfaces;
using InsightBase.Application.Models.Enum;
using InsightBase.Application.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio.DataModel;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using UglyToad.PdfPig.Content;

namespace InsightBase.Infrastructure.Services.RAG
{
    public partial class AnswerValidator : IAnswerValidator
    {
        private readonly ILLMClient _llmClient;
        private readonly ILogger<AnswerValidator> _logger;
        private readonly IConfiguration _config;
        public AnswerValidator(ILLMClient llmClient, ILogger<AnswerValidator> logger, IConfiguration config) => (_llmClient, _logger, _config) = (llmClient, logger, config);
        public async Task<ValidationResult> ValidateAsync(string llmAnswer, List<SearchResult> sources, QueryContext queryContext, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Cevap doğrulaması başladı.");

            var result = new ValidationResult
            {
                IsValid = true,
                Answer = llmAnswer
            };

            // 1. Citation kontrolü
            ValidateCitations(llmAnswer, sources, result);

            // 2. Law reference kontrolü
            ValidateLawReferences(llmAnswer, sources, result);

            // 3. Date kontrolü
            ValidateDates(llmAnswer, sources, result);

            // 4. Court/organization name kontrolü
            ValidateCourtNames(llmAnswer, sources, result);

            // ?????????????????????????????????????????????????????????
            var enableLLMValid = _config["RAG:Validation:EnableLLMValidation"];
            // 5. Hallucination detection (LLM ile)
            if (Convert.ToBoolean(enableLLMValid)) //EnableLLMValidation
            {
                await DetectHallucinationAsync(llmAnswer, sources, result, cancellationToken);
            }

            // 6. Content grounding check
            ValidateContentGrounding(llmAnswer, sources, result);

            // 7. Legal disclaimer check
            ValidateLegalDisclaimer(llmAnswer, result);

            // Final karar
            result.IsValid = !result.Errors.Any() && result.Warnings.Count <= Convert.ToInt32(_config["RAG:Validation:MaxWarningsAllowed"]); //_config.MaxWarningsAllowed;

            _logger.LogInformation(
                "Doğrulama bitti. Doğru: {Valid}, Hatalar: {Errors}, Uyarılar: {Warnings}",
                result.IsValid, result.Errors.Count, result.Warnings.Count);

            return result;
        }


        private void ValidateCitations(string llmAnswer, List<SearchResult> sources, ValidationResult result)
        {
            // [KAYNAK-X] formatındaki citation bulur
            var citationRegex = CitationPattern();
            var citations = citationRegex.Matches(llmAnswer);
            if(!citations.Any())
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Type = WarningType.MissingCitation,
                    Message = "Yanıtta hiç kaynak referansı bulunamadı.",
                    Severity = Severity.High
                });
                return;
            }

            foreach (Match citation in citations)
            {
                var indexStr = citation.Groups[1].Value;
                if(int.TryParse(indexStr, out int index))
                {
                    if(index < 1 || index > sources.Count)
                    {
                        result.Errors.Add(new ValidationError
                        {
                            Type = ErrorType.InvalidCitation,
                            Message = $"Geçersiz kaynak referansı: [KAYNAK-{index}]. Kaynak sayısı: {sources.Count}",
                            Location = citation.Index
                        });
                    }
                }
            }

            var sentenceCount = llmAnswer.Split('.', '!', '?').Length;
            var citationRatio = (double)citations.Count / sentenceCount;
            if(citationRatio < 0.2)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Type = WarningType.LowCitationDensity,
                    Message = "Kaynak referansları yetersiz. Daha fazla atıf yapılmalı.",
                    Severity = Severity.Medium
                });
            }
        }
        private void ValidateLawReferences(string llmAnswer, List<SearchResult> sources, ValidationResult result)
        {
            // TCK 86, TBK 49 gibi referansları bul
            var lawRefRegex = LawReferencePattern();
            var referaecesInAnswer = lawRefRegex.Matches(llmAnswer)
                                                    .Select(m => m.Value)
                                                    .Distinct()
                                                    .ToList();

            // kaynaklarda geçen tüm law references
            var referencesInSources = sources
                                        .SelectMany(s => s.LawReferences)        
                                        .Distinct()
                                        .ToList();
            
            foreach(var reference in referaecesInAnswer)
            {
                if(!referencesInSources.Any(r =>
                    r.Contains(reference, StringComparison.OrdinalIgnoreCase)))
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Type = WarningType.UngroundedLawReference,
                        Message = $"Kanun referansı kaynaklarda bulunamadı: {reference}",
                        Severity = Severity.High,
                        Details = reference
                    });
                }
            }
        }
        private void ValidateDates(string llmAnswer, List<SearchResult> sources, ValidationResult result)
        {
            // tarih 02.12.2025, 2025
            var dateRegex = DatePattern();
            var datesInAnswer = dateRegex.Matches(llmAnswer);

            foreach (Match dateMatch in datesInAnswer)
            {
                var dateStr = dateMatch.Value;
                // gelecek tarih kontrolü
                if(DateTime.TryParse(dateStr, out var date))
                {
                    if(date > DateTime.UtcNow)
                    {
                        result.Errors.Add(new ValidationError
                        {
                            Type = ErrorType.FutureDate,
                            Message = $"Gelecek tarih kullanılmış: {dateStr}",
                            Location = dateMatch.Index
                        });
                    }

                    var foundInSources = sources.Any(s => 
                            s.PublishDate.HasValue && 
                            Math.Abs((s.PublishDate.Value - date).TotalDays) < 365);

                    if(!foundInSources)
                    {
                        result.Warnings.Add(new ValidationWarning
                        {
                            Type = WarningType.UngroundedDate,
                            Message = $"Tarih kaynaklarda doğrulanamadı: {dateStr}",
                            Severity = Severity.Medium,
                            Details = dateStr
                        });
                    }
                }
                
            }
        }
        private void ValidateCourtNames(string llmAnswer, List<SearchResult> sources, ValidationResult result)
        {
            var courtRegex = CourtPattern();
            // yargıtay, danıştay ... 
            var courtsInAnswer = courtRegex.Matches(llmAnswer)
                                    .Select(m => m.Value)
                                    .Distinct()
                                    .ToList();

            var courtsInSources = sources
                                    .Where(s => !string.IsNullOrEmpty(s.Court))
                                    .Select(m => m.Court)
                                    .Distinct()
                                    .ToList();

            foreach (var court in courtsInAnswer)
            {
                if (!courtsInSources.Any(c => 
                    c.Contains(court, StringComparison.OrdinalIgnoreCase)))
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Type = WarningType.UngroundedCourt,
                        Message = $"Mahkeme/daire kaynaklarda bulunamadı: {court}",
                        Severity = Severity.Medium,
                        Details = court
                    });
                }
            }
        }   
        private void ValidateContentGrounding(string llmAnswer, List<SearchResult> sources, ValidationResult result)
        {
            var answerSentences = llmAnswer.Split('.', '!', '?')
                                        .Where(s => !string.IsNullOrWhiteSpace(s))
                                        .Select(s => s.Trim())
                                        .ToList();
            
            var ungroundedCount = 0;
            var totalSentences = answerSentences.Count;

            foreach (var sentence in answerSentences)
            {
                // bu cümle kaynaklarda geçiyo mu? (basit benzerlik)
                var isGrounded = sources.Any(s => 
                        CalculateSimilarity(sentence, s.Content) > 0.3);
                if(!isGrounded && !IsGenericSentence(sentence))
                {
                    ungroundedCount++;
                }
            }

            var ungroundedRatio = (double)ungroundedCount / totalSentences;
            if(ungroundedRatio > 0.3) // %30 dan fazlası kaynaklara dayanmıyor
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Type = WarningType.PoorGrounding,
                    Message = $"Yanıtın %{ungroundedRatio:P0}'si kaynaklara dayanmıyor olabilir",
                    Severity = Severity.High
                });
            }
        }
        private void ValidateLegalDisclaimer(string llmAnswer, ValidationResult result)
        {
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            var requireLegalDisclaimer = _config["RAG:Validation:RequireLegalDisclaimer"];
            if(!Convert.ToBoolean(requireLegalDisclaimer)) return;

            var disclaimerKeywords = new[]
            {
                "hukuki görüş", "kesin hukuki danışma değil",
                "avukata danış", "hukuki yardım alınmalı"
            };

            var hasDisclaimer = disclaimerKeywords.Any(keyword => 
                    llmAnswer.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            if(!hasDisclaimer)
            {
                result.Warnings.Add(new ValidationWarning
                {
                   Type = WarningType.MissingDisclaimer,
                   Message ="Yanıtta hukuki sorumluluk uyarısı eksik",
                    Severity = Severity.Medium
                });
            }
        }


        private async Task DetectHallucinationAsync(string llmAnswer, List<SearchResult> sources, ValidationResult result, CancellationToken cancellationToken)
        {
            try
            {
                var prompt = BuildHallucinationDetectionPrompt(llmAnswer,sources);
                var llmResponse = await _llmClient.GenerateJsonResponseAsync(
                                                        prompt,
                                                        llmAnswer,
                                                        cancellationToken
                );
                if(llmResponse.Fields.TryGetValue("has_hallucination", out var hasHallucination))
                {
                    if(bool.Parse(hasHallucination))
                    {
                        result.Errors.Add(new ValidationError
                        {
                            Type = ErrorType.Hallucination,
                            Message = "LLM kaynaklarda olmayan bilgi üretmiş olabilir",
                            Details = llmResponse.Fields.GetValueOrDefault("hallucination_details", "")
                        });
                    }
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning(ex, "AnswerValidator DetectHallucinationAsync: Halüsinasyon tarama başarısız oldu.");
                result.Warnings.Add(new ValidationWarning
                {
                    Type = WarningType.ValidationFailure,
                    Message = "Halüsinasyontespiti tarama başarısız oldu.",
                    Severity = Severity.Low
                });
            }
        }
        private string BuildHallucinationDetectionPrompt(string llmAnswer, List<SearchResult> sources)
        {
            
            return $@"
                Aşağıdaki LLM yanıtını verilen kaynaklarla karşılaştır ve hallucination olup olmadığını tespit et.

                KAYNAKLAR:
                {string.Join("\n\n", sources.Select((s, i) => $"[{i + 1}] {s.Content}"))}

                LLM YANITI:
                {llmAnswer}

                JSON formatında döndür:
                {{
                    ""has_hallucination"": true/false,
                    ""hallucination_details"": ""eğer varsa detaylar"",
                    ""confidence"": 0.0-1.0
                }}";
        }
        private double CalculateSimilarity(string sentence, string content)
        {
            var words1 = sentence.ToLower().Split(' ').ToHashSet();
            var words2 = content.ToLower().Split(' ').ToHashSet();
            var intersection = words1.Intersect(words2).Count();
            var union = words1.Union(words2).Count();
            return union > 0 ? (double)intersection / union : 0;
        }
        private bool IsGenericSentence(string sentence)
        {
            var genericPhrases = new[]
            {
                "sonuç olarak", "özetle", "bu nedenle", "dolayısıyla",
                "yukarıda belirtildiği", "aşağıda açıklandığı"
            };
            return genericPhrases.Any(phrase => 
                sentence.Contains(phrase, StringComparison.OrdinalIgnoreCase));
        }

        [GeneratedRegex(@"\[KAYNAK-(\d+)\]")]
        private static partial Regex CitationPattern();

        [GeneratedRegex(@"(TCK|TBK|CMK|HMK|TMK|İİK|TTK)\s*\d+")]
        private static partial Regex LawReferencePattern();

        [GeneratedRegex(@"\d{2}\.\d{2}\.\d{4}|\d{4}")]
        private static partial Regex DatePattern();

        [GeneratedRegex(@"Yargıtay\s+\d+\.?\s*(?:Ceza|Hukuk)\s+Dairesi")]
        private static partial Regex CourtPattern();
    }
}