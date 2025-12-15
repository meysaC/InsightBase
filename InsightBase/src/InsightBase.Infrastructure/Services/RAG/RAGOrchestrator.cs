using InsightBase.Application.Interfaces;
using InsightBase.Application.Models;
using InsightBase.Application.Models.Enum;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InsightBase.Infrastructure.Services.RAG
{
    public class RAGOrchestrator : IRAGOrchestrator // ana RAG pipeline
                                // tüm servisleri koordine eder: Query Analysis → Search → Ranking → Prompt → LLM → Validation
    {
        private readonly IQueryAnalyzer _queryAnalyzer;
        private readonly IHybridSearchService _searchService;
        private readonly IAccessControlService _accessControl;
        private readonly IPromptBuilder _promptBuilder;
        private readonly ILLMClient _llmClient;
        private readonly IAnswerValidator _answerValidator;
        private readonly ICitationMapper _citationMapper;
        private readonly ILogger<RAGOrchestrator> _logger;
        private readonly IConfiguration _configuration;
        public RAGOrchestrator(
            IQueryAnalyzer queryAnalyzer,
            IHybridSearchService searchService,
            IAccessControlService accessControl,
            IPromptBuilder promptBuilder,
            ILLMClient llmClient,
            IAnswerValidator answerValidator,
            ICitationMapper citationMapper,
            ILogger<RAGOrchestrator> logger,
            IConfiguration configuration
            )
        {
            _queryAnalyzer = queryAnalyzer;
            _searchService = searchService;
            _accessControl = accessControl;
            _promptBuilder = promptBuilder;
            _llmClient = llmClient;
            _answerValidator = answerValidator;
            _citationMapper = citationMapper;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<RAGResponse> GenerateAnswerAsync(string userQuery, string? userId, RAGOptions? options = null, CancellationToken cancellationToken = default) // ana RAG pipeline
        {
            options ??= new RAGOptions();
            var startTime = DateTime.UtcNow;

            _logger.LogInformation("RAG Answer Generation started at {StartTime} for User: {UserId}", startTime, userId ?? "Anonymous");

            var response = new RAGResponse
            {
                Query = userQuery,
                UserId = userId ?? "Anonymous",
                StartTime = startTime
            };

            try
            {
                // 1. query analiz
                _logger.LogInformation("Step 1/7: Analyzing query.");
                var queryContext = await _queryAnalyzer.AnalyzeAsync(userQuery, userId, cancellationToken);
                response.QueryContext = queryContext;
                if(!queryContext.IsValid)
                {
                    response.Success = false;
                    response.ErrorMessage = "Query analysis failed.";
                    return response;
                }

                // 2. acces control
                _logger.LogInformation("Step 2/7: Checking access control.");
                var accessDomain = await _accessControl.BuildAccessDomainAsync(userId, cancellationToken);

                // 3. hybrid search
                _logger.LogInformation("Step 3/7: Performing hybrid search.");
                var searchResults = await _searchService.SearchAsync(queryContext, userId, cancellationToken);
                if(!searchResults.Any())
                {
                    response.Success = false;
                    response.ErrorMessage = "No relevant sources found.";
                    response.Answer = "Üzgünüm, sorunuzla ilgili kaynak bulunamadı.";
                    return response;
                }

                response.SourceCount = searchResults.Count;
                _logger.LogInformation("Found {SourceCount} sources.", response.SourceCount);

                // 4. prompt oluşturma
                _logger.LogInformation("Step 4/7: Building prompt.");
                var prompt = _promptBuilder.BuildPrompt(queryContext, searchResults, userQuery);

                // 5. llm generate answer
                _logger.LogInformation("Step 5/7: Generating answer from LLM.");
                var llmResponse = await _llmClient.GenerateJsonResponseAsync(prompt, userQuery, cancellationToken);
                var rawAnswer = llmResponse.RawJson;

                // 6. answer validation
                _logger.LogInformation("Step 6/7: Validating answer.");
                var validationResult = await _answerValidator.ValidateAsync(rawAnswer, searchResults, queryContext, cancellationToken);
                if(!validationResult.IsValid && _configuration.GetValue<bool>("RAG:RAGOrchestrator:RejectInvalidAnswers"))
                {
                    _logger.LogWarning("Answer validation failed with {ErrorCount} errors.", validationResult.Errors.Count);

                    //retry with stricter prompt
                    if(_configuration.GetValue<bool>("RAG:RAGOrchestractor:RetryOnValidationFailure"))
                    {
                        _logger.LogInformation("Retrying answer generation with stricter prompt.");
                        rawAnswer = await RetryWithStricterPromptAsync(prompt, searchResults, validationResult, cancellationToken);

                        // re-validate
                        validationResult = await _answerValidator.ValidateAsync(rawAnswer, searchResults, queryContext, cancellationToken);
                    }
                }
                response.ValidationResult = validationResult;

                // 7. citation mapping
                _logger.LogInformation("Step 7/7: Mapping citations.");
                var citationMapping = _citationMapper.MapCitations(rawAnswer, searchResults);
                {
                    response.Answer = citationMapping.OriginalAnswer;
                    response.Citations = citationMapping.Citations;
                    response.Sources = searchResults.Take(10).ToList(); // max 10 source

                    //citation summary
                    response.CitationSummary = _citationMapper.BuildCitationSummary(citationMapping.Citations);

                    response.Success = true;
                    response.EndTime = DateTime.UtcNow;
                    response.TotalDuration = response.EndTime - response.StartTime;
                }
                
                _logger.LogInformation("RAG Answer Generation completed at {EndTime}. Total Duration: {Duration} seconds. Citations: {CitationCount}", response.EndTime, response.TotalDuration.TotalSeconds, citationMapping.TotalCitations);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during RAG Answer Generation for User: {UserId}", userId ?? "Anonymous");
                response.Success = false;
                response.ErrorMessage = ex.Message;
                response.Answer = "Üzgünüm, sorunuz işlenirken bir hata oluştu.";
                response.EndTime = DateTime.UtcNow;
                response.TotalDuration = response.EndTime - response.StartTime;
                return response;
            }
        }
        public async IAsyncEnumerable<RAGStreamChunk> GenerateAnswerStreamAsync(string userQuery, string? userId, RAGOptions? options = null, CancellationToken cancellationToken = default) // ana RAG pipeline - stream versiyonu
        {
            // query anlysis
            yield return new RAGStreamChunk
            {
                Type = StreamChunkType.Status,
                Content = "Sorgu analiz ediliyor..."
            };
            var queryContext = await _queryAnalyzer.AnalyzeAsync(userQuery, userId, cancellationToken);

            yield return new RAGStreamChunk
            {
                Type = StreamChunkType.Status,
                Content = "Kaynaklar aranıyor..."
            };
            var accessDomain = await _accessControl.BuildAccessDomainAsync(userId, cancellationToken);

            var searchResults = await _searchService.SearchAsync(queryContext, userId, cancellationToken);

            yield return new RAGStreamChunk
            {
                Type = StreamChunkType.Sources,
                Content = $"{searchResults.Count} kaynak bulundu.",
                Metadata = new {SourceCount = searchResults.Count}
            };

            // generate answer
            yield return new RAGStreamChunk
            {
                Type = StreamChunkType.Status,
                Content = "Cevap oluşturuluyor..."
            };

            // LLM streaming implementation buraya gelecek
            // Şu an için basit versiyonu döndürüyoruz  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            var prompt = _promptBuilder.BuildPrompt(queryContext, searchResults, userQuery);
            var llmResponse = await _llmClient.GenerateJsonResponseAsync(prompt, userQuery, cancellationToken);
            yield return new RAGStreamChunk
            {
                Type =StreamChunkType.Answer,
                Content = llmResponse.RawJson
            };


            var citationMapping = _citationMapper.MapCitations(llmResponse.RawJson, searchResults);            
            yield return new RAGStreamChunk
            {
                Type = StreamChunkType.Citations,
                Content = _citationMapper.BuildCitationSummary(citationMapping.Citations),
                Metadata = new {Citations = citationMapping.Citations}
            };

            yield return new RAGStreamChunk
            {
                Type = StreamChunkType.Complete,
                Content = "Tamamnalndı."
            };
        }


        private async Task<string> RetryWithStricterPromptAsync(string prompt, List<SearchResult> searchResults, ValidationResult validationResult, CancellationToken cancellationToken)
        {
            var stricterPrompt = $@"
                    {prompt}

                    **ÖNEMLİ UYARILAR:**

                    Bir önceki yanıtınızda aşağıdaki sorunlar tespit edildi:
                    {string.Join("\n", validationResult.Errors.Select(e => $"- {e.Message}"))}
                    {string.Join("\n", validationResult.Warnings.Select(w => $"- {w.Message}"))}

                    Bu sefer:
                    1. Her ifade için mutlaka [KAYNAK-X] referansı kullan
                    2. Kaynaklarda olmayan bilgi verme
                    3. Tarih ve madde numaralarını çok dikkatli kontrol et
                    4. Yalnızca verilen kaynaklara dayanarak cevap ver
                    ";

            var response = await _llmClient.GenerateJsonResponseAsync(
                stricterPrompt,
                "Lütfen yukarıdaki uyarıları dikkate alarak cevabı yeniden oluştur.",
                cancellationToken);

            return response.RawJson;
        }
    }
}