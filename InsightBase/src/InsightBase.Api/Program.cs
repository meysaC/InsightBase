using InsightBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

using InsightBase.Application;
using InsightBase.Infrastructure;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.Load(); // 1. root'taki .env dosyasını yükle
builder.Configuration.AddEnvironmentVariables(); // 2. Environment değişkenlerini config'e ekle

// Application ve Infrastructure katmanlarını ekle
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ReportApiVersions = true; // header'da hangi versiyonların mevcut olduğunu döndür
});
builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "InsigthBase Demo", Version = "v1" });

});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    app.UseSwagger();
    app.UseSwaggerUI(opt =>
    {
        foreach (var desc in provider.ApiVersionDescriptions)
        {
            opt.SwaggerEndpoint($"/swagger/{desc.GroupName}/swagger.json", desc.GroupName.ToUpperInvariant());
        }
        opt.RoutePrefix = "swagger"; //string.Empty "launchUrl": "swagger",
    });
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();