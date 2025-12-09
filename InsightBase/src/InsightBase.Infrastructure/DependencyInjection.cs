using InsightBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using InsightBase.Application.Interfaces;
using InsightBase.Infrastructure.Services;
using InsightBase.Infrastructure.Repositories;
using OpenAI.Extensions;
using Minio;
using InsightBase.Infrastructure.Workers;
using InsightBase.Infrastructure.Services.Search;
using InsightBase.Infrastructure.Services.RAG;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace InsightBase.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // DB connection burada register edilir
            var conn = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(conn)) throw new InvalidOperationException("Connection string not found.");
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(conn, npg => npg.UseVector())
            );
            
            services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();
            
            // services.AddAuthentication(options =>
            // {
            //     options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            //     options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            //     options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            // })
            // .AddJwtBearer(options =>
            // {
            //     options.TokenValidationParameters = new TokenValidationParameters
            //     {
            //         ValidateIssuer = true,
            //         ValidIssuer = issuer,
            //         ValidateAudience = true,
            //         ValidAudience = audience,
            //         ValidateIssuerSigningKey = true,
            //         IssuerSigningKey = new SymmetricSecurityKey(
            //             Encoding.UTF8.GetBytes(signingKey)),
            //         ValidateLifetime = true
            //     };

            //     options.Events = new JwtBearerEvents
            //     {
            //         OnAuthenticationFailed = context =>
            //         {
            //             Console.WriteLine($"JWT Authentication failed: {context.Exception.Message}");
            //             return Task.CompletedTask;
            //         }
            //     };
            // });
            // JWT authentication ayarları burada eklenebilir (services.AddAuthentication... gibi)
            return services;
        }
        
        // bağımlılıkları ayrıştırarak daha sağlam bir mimari oluşturursun.
        // Eğer ileride microservice tarzı ayrıştırmaya gidersen, şimdiden Persistence ve InfrastructureServices ayrımı yapmak bölünebilirliği kolaylaştırır.
        // büyük projelerde, lazy-load veya conditionally load yapısı kurmaya da kapı açar.
        // daha az memory footprint.
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // services.AddOpenAIService(options =>
            // {
            //     options.ApiKey = configuration["OpenAI:ApiKey"];
            // });

            services.AddScoped<IEmbeddingService, EmbeddingService>();
            services.AddScoped<VectorDbService>();
            services.AddScoped<IStorageService, MinioStorageService>();
            services.AddScoped<ITextExtractionService, TextExtractionService>();
            services.AddSingleton<MinioClient>();
            services.AddScoped<IChunkingService, TokenBasedChunkingService>();
            services.AddScoped<IDocumentRepository, DocumentRepository>();
            services.AddScoped<IEmbeddingRepository, EmbeddingRepository>();
            services.AddScoped<IRedisCacheService, RedisCacheService>();
            services.AddSingleton<IMessageBus, RabbitMqMessageBus>();
            services.AddHostedService<EmbeddingWorker>(); //HostedService kendiliğinden Singleton gibi davranır
            services.AddScoped<IHybridSearchService, HybridSearchService>();
            services.AddScoped<IAnswerValidator, AnswerValidator>();
            services.AddScoped<IPromptBuilder, PromptBuilder>();


            
            return services;
        }

    }
}