using InsightBase.Application.Interfaces;
using InsightBase.Infrastructure.Persistence;
using InsightBase.Infrastructure.Services.QueryAnalyzer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http;
using System.Net.Http.Headers;


namespace InsightBase.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddQueryAnalyzer(this IServiceCollection services, IConfiguration configuration)
        {
            var llmApiUrl = configuration["LLM_API_URL"] ?? throw new InvalidOperationException("LLM:ApiUrl eksik ya da yanlış.");
            var llmTimeout = configuration.GetValue<int>("LLM_TIMEOUT_SECONDS", 30);
            var llmRetrycount = configuration.GetValue<int>("LLM_RETRY_COUNT", 3);

            services.AddSingleton<RegexExtractor>();

            services.AddHttpClient<ILLMClient, LLMClient>(client =>
            {
                client.BaseAddress = new Uri(llmApiUrl);
                client.Timeout = TimeSpan.FromSeconds(llmTimeout); // HttpClient için üst seviye timeout
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                var apiKey = configuration["OpenAI:ApiKey"]; //OpenAI__ApiKey
                if(!string.IsNullOrWhiteSpace(apiKey))
                {
                    // client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}"); 
                    // OpenAI API bazı client’larda bu formatı strict kontrol eder ve Add() yerine AuthenticationHeaderValue ister !!!!!
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", apiKey);

                }

            })
            .AddPolicyHandler(GetRetryPolicy(llmRetrycount))
            .AddPolicyHandler(GetCircuitBreakerPolicy(configuration));

            services.AddScoped<ILLMExtractor, LLMExtractor>();
            services.AddScoped<IQueryAnalyzer, QueryAnalyzer>();

            return services;
        }
        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(IConfiguration config)
        {
            var failures = config.GetValue<int>("LLM_CIRCUIT_BREAKER_FAILURE_THRESHOLD", 5);
            var breakSeconds = config.GetValue<int>("LLM_CIRCUIT_BREAK_DURATION_SECONDS", 30);

            // Eğer LLM API art arda X kez hata verirse → circuit “Open” durumuna geçer -> 30 saniye boyunca hiç istek gönderilmez.
            return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .CircuitBreakerAsync(
                        handledEventsAllowedBeforeBreaking: failures,
                        durationOfBreak: TimeSpan.FromSeconds(breakSeconds),
                        onBreak:  (outcome, duration) =>
                            {
                                Console.WriteLine($"Circuit breaker opened for {duration.TotalSeconds}s");
                            },
                            onReset: () =>
                            {
                                Console.WriteLine("Circuit breaker reset");
                            }
                    );
        }
        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int llmRetrycount)
        {
            // retry policy 3 deneme, exponential backoff
            // HandleTransientHttpError() retry çalıştırır -> 5xx hata kodları, 408 Timeout, HttpRequestException
            return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests) //LLM API’leri rate-limit yaptığı için 429 hatası özel eklendi
                    .WaitAndRetryAsync(
                        retryCount: llmRetrycount,
                        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff (sistemi yormadan yeniden deneme) -> 2, 4, 8 saniye
                        onRetry: (outcome, timespan, retryAttempt, context) =>
                        {
                            Console.WriteLine($"Retry {retryAttempt} after {timespan.TotalSeconds}s due to {outcome.Exception?.Message ?? outcome.Result?.ReasonPhrase}");
                        }
                    );
        }
        public static IServiceCollection AddQueryAnalyzerWithProvider(this IServiceCollection services, IConfiguration config) //LLMProvider provider
        {
            services.AddQueryAnalyzer(config);
            
            // string provider = config["LLM_PROVIDER"] ?? "OpenAI";
            // string model = config["LLM_MODEL"] ?? "gpt-4o-mini";

            services.Configure<LLMOptions>(options =>
            {
                options.Provider = "OpenAI";
                options.Model = "gpt-4o-mini";
                options.MaxTokens = 1000;
                options.Temperature = 0.1;
            });


            // switch (provider)
            // {
            //     case LLMProvider.OpenAI:
            //         services.Configure<LLMOptions>(options =>
            //         {
            //             options.Provider = "OpenAI";
            //             options.Model = "gpt-4o-mini";
            //             options.MaxTokens = 1000;
            //             options.Temperature = 0.1;
            //         });
            //         break;

            //     case LLMProvider.Claude:
            //         services.Configure<LLMOptions>(options =>
            //         {
            //             options.Provider = "Claude";
            //             options.Model = "claude-3-5-sonnet-20241022";
            //             options.MaxTokens = 1000;
            //             options.Temperature = 0.1;
            //         });
            //         break;

            //     case LLMProvider.AzureOpenAI:
            //         services.Configure<LLMOptions>(options =>
            //         {
            //             options.Provider = "AzureOpenAI";
            //             options.Model = "gpt-4";
            //             options.MaxTokens = 1000;
            //             options.Temperature = 0.1;
            //         });
            //         break;
            // }

            return services;
        }
    }
}