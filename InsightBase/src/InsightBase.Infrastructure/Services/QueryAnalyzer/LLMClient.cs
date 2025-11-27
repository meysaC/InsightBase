using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using InsightBase.Application.Interfaces;
using InsightBase.Application.Models;
using iText.StyledXmlParser.Jsoup.Nodes;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using UglyToad.PdfPig.Logging;


namespace InsightBase.Infrastructure.Services.QueryAnalyzer
{
    public class LLMClient : ILLMClient // di üzerinden gelen HttpClient ile istek atıyor, Polly kullanarak transient (geçici) hatalarda tekrar deniyor 
                                        // ve API’den dönen JSON’u valide edip düzenli bir forma çeviriyor
                                        // LLM API ile iletişim kurar (with retry logic, circuit breaker and error handling)
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<LLMClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        // private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy; 

        public LLMClient(HttpClient httpClient, ILogger<LLMClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _jsonOptions = new JsonSerializerOptions
            {
              PropertyNameCaseInsensitive = true, //JSON’daki key’ler büyük/küçük harfe takılmaz
              WriteIndented = false,
              DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull // null döndürmez
            };

            // // retry policy 3 deneme, exponential backoff
            // // HandleTransientHttpError() retry çalıştırır -> 5xx hata kodları, 408 Timeout, HttpRequestException
            // _retryPolicy = HttpPolicyExtensions
            //     .HandleTransientHttpError()
            //     .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests) //LLM API’leri rate-limit yaptığı için 429 hatası özel eklendi
            //     .WaitAndRetryAsync(
            //         retryCount: 3,
            //         sleepDurationProvider: retryAttempt => 
            //             TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff (sistemi yormadan yeniden deneme) -> 2, 4, 8 saniye
            //         onRetry: (outcome, timespan, retryCount, context) => // Retrylerin neden, kaçıncı denemede yapıldığını kaydediyor
            //         {
            //             _logger.LogWarning("LLMClient LLM API: Deneme {RetryCount} - {StatusCode}. Sonraki deneme {Delay} saniye sonra. {Message}",
            //                 retryCount,
            //                 outcome.Result?.StatusCode,
            //                 timespan.TotalSeconds,
            //                 outcome.Exception?.Message ?? outcome.Result?.ReasonPhrase);
            //         });
        }


        public async Task<LLMJsonResponse> GenerateJsonResponseAsync(string instruction, string input, CancellationToken cancellationToken = default) // LLM API'den JSON yanıtı alır
        {
            ValidateInput(instruction, input);

            var requestBody = BuildRequestBody(instruction, input);

            try
            {
                // HttpClient policy (retry + circuit breaker) hali hazırda di ile uygulandı
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(30));

                var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
                _logger.LogDebug("LLMClient LLM request body: {RequestBody}", json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("chat/completions", content, cts.Token);

                // hata durumunda detaylı log
                if(!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("LLMClient LLM API Error: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
                    response.EnsureSuccessStatusCode();
                }

                return await ProcessResponse(response, cancellationToken);
            }
            catch (TaskCanceledException ex) // Timeout veya kullanıcı tarafından istek iptal edilmesi
            {
                _logger.LogError(ex, "LLMClient GenerateJsonResponseAsync: İstek zaman aşımına uğradı veya iptal edildi.");
                throw new TimeoutException("LLMClient GenerateJsonResponseAsync: İstek zaman aşımına uğradı veya iptal edildi.", ex);
            }
            catch (HttpRequestException ex) // Network error, DNS error, unreachable host, vs
            {
                _logger.LogError(ex, "LLMClient GenerateJsonResponseAsync: LLM API isteği sırasında beklenmeyen bir hata oluştu.");
                throw new ApplicationException("LLMClient GenerateJsonResponseAsync: LLM API isteği sırasında beklenmeyen bir hata oluştu.", ex);
            }

        }

        private static void ValidateInput(string instruction, string input) // Giriş parametrelerini doğrular
        {
            if (string.IsNullOrWhiteSpace(instruction))
                throw new ArgumentException("LLMClient ValidateInput: Instruction boş olamaz.", nameof(instruction));

            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("LLMClient ValidateInput: Input boş olamaz.", nameof(input));

            // yaklaşık token limiti kontrol et (basit tahmin: 1 token ~ 4 karakter)
            const int maxTokens = 4000; //GPT-4 için güvenli limit
            int estimatedTokens = (instruction.Length + input.Length) / 4;

            if (estimatedTokens > maxTokens)
                throw new ArgumentException($"LLMClient ValidateInput: Giriş çok uzun. Yaklaşık token sayısı {estimatedTokens}, maksimum izin verilen {maxTokens} token.", nameof(input));
        }

        private static object BuildRequestBody(string instruction, string input) // İstek gövdesini oluşturur
        {
            return new
            {
                model = "gpt-4o-mini", //gpt-4.1-mini
                messages = new []
                {
                    new { role = "system", content = instruction },
                    new { role = "user", content = input }
                },
                response_format = new { type = "json_object" },
                max_tokens = 1000,
                temperature = 0.1
            };
        }

        private async Task<LLMJsonResponse> ProcessResponse(HttpResponseMessage response, CancellationToken cancellationToken) // HTTP responsunu LLMJsonResponse dönüştürür
        {
            if(!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("LLMClient ProcessResponse: LLM API hatası. StatusCode: {StatusCode}, Content: {Content}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"LLMClient ProcessResponse: LLM API hatası. StatusCode: {response.StatusCode}, Content: {errorContent}");
            }


            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            if(string.IsNullOrWhiteSpace(jsonResponse))
            {
                _logger.LogError("LLMClient ProcessResponse: LLM API boş yanıt döndü.");
                throw new HttpRequestException("LLMClient ProcessResponse: LLM API boş yanıt döndü.");
            }

            // json validation
            try
            {
                // AI kaynaklı servislerde mutlaka JSON validasyon yapılmalı 
                var testParse = JsonDocument.Parse(jsonResponse); // JSON valid mi, kırık mı...
                testParse.Dispose();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "LLMClient ProcessResponse: LLM API'den geçersiz JSON: {Json}", jsonResponse);
                throw new ApplicationException("LLMClient ProcessResponse: LLM API'den geçersiz JSON", ex);
            }


            var apiResponse = JsonSerializer.Deserialize<JsonElement>(jsonResponse, _jsonOptions);
            var contentJson = apiResponse
                            .GetProperty("choices")[0]
                            .GetProperty("message")
                            .GetProperty("content")
                            .GetString() ?? string.Empty;
            _logger.LogDebug("LLMClient ProcessResponse çıkarılan content: {Content}", contentJson);

            // content temizleniyor, bazen llm markdown code block ile sarabilir
            contentJson = contentJson.Trim();
            if(contentJson.StartsWith("```json")) contentJson = contentJson.Substring(7);
            if(contentJson.StartsWith("```")) contentJson = contentJson.Substring(3);
            if(contentJson.EndsWith("```")) contentJson = contentJson.Substring(0, contentJson.Length - 3);
            contentJson = contentJson.Trim();

            var fields = new Dictionary<string, string>();
            try
            {
                // using var doc = JsonDocument.Parse(jsonResponse);
                var contentElement = JsonSerializer.Deserialize<JsonElement>(contentJson, _jsonOptions);
                ExtractFieldsRecursive(contentElement, string.Empty, fields); //doc.RootElement, ""
            }
            catch (Exception ex)
            {
                _logger.LogWarning("LLMClient ProcessResponse: JSON'dan alan çıkarılırken hata oluştu.", ex);
            }

            return new LLMJsonResponse
            {
                RawJson = contentJson, // jsonResponse SADECE CONTENT JSON KAYDEDİLİYOR
                Fields = fields,
                Timestamp = DateTime.UtcNow
            };
        }

        private void ExtractFieldsRecursive(JsonElement rootElement, string prefix, Dictionary<string, string> fields) // JSON'dan tüm fieldları recursive olarak çıkarır
        {
            switch(rootElement.ValueKind)
            {
                case JsonValueKind.Object: // her property i gezer → key → value
                    foreach (var property in rootElement.EnumerateObject())
                    {
                        var key = string.IsNullOrEmpty(prefix)
                                ? property.Name
                                : $"{prefix}.{property.Name}";
                        ExtractFieldsRecursive(property.Value, key, fields);
                    }
                    break;
                case JsonValueKind.Array: // diziyi string listesine çevirir
                    var arrayValues = rootElement.EnumerateArray()
                                    .Select(e => e.ToString())
                                    .ToList();
                    fields[prefix] = string.Join(",", arrayValues);
                    break;
                case JsonValueKind.String:
                    fields[prefix] = rootElement.GetString() ?? string.Empty;
                    break;
                case JsonValueKind.Number:
                    fields[prefix] = rootElement.GetRawText();
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    fields[prefix] = rootElement.GetBoolean().ToString();
                    break;
                case JsonValueKind.Null:
                    fields[prefix] = string.Empty;
                    break;
                
            }
        }

    }
}