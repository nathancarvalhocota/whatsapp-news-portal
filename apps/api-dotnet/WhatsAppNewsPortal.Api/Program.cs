using Microsoft.EntityFrameworkCore;
using WhatsAppNewsPortal.Api.AiGeneration.Application;
using WhatsAppNewsPortal.Api.AiGeneration.Infrastructure;
using WhatsAppNewsPortal.Api.Articles.Application;
using WhatsAppNewsPortal.Api.Articles.Infrastructure;
using WhatsAppNewsPortal.Api.Infrastructure.Data;
using WhatsAppNewsPortal.Api.ContentProcessing.Application;
using WhatsAppNewsPortal.Api.ContentProcessing.Infrastructure;
using WhatsAppNewsPortal.Api.Ingestion.Application;
using WhatsAppNewsPortal.Api.Ingestion.Infrastructure;
using WhatsAppNewsPortal.Api.Sources.Application;
using WhatsAppNewsPortal.Api.Sources.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// --- Logging estruturado ---
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// --- PostgreSQL + EF Core ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["DATABASE_URL"]
    ?? throw new InvalidOperationException("Connection string not configured. Set ConnectionStrings__DefaultConnection or DATABASE_URL.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// --- HTTP Clients ---
builder.Services.AddHttpClient<RssIngestionAdapter>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("WhatsAppNewsPortal/1.0");
});
builder.Services.AddScoped<IIngestionAdapter, RssIngestionAdapter>();

builder.Services.AddHttpClient<IHtmlFetcher, HtmlFetcher>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("WhatsAppNewsPortal/1.0");
});
builder.Services.AddScoped<HtmlIngestionAdapter>();

// --- ContentProcessing ---
builder.Services.AddScoped<ISourceItemRepository, EfSourceItemRepository>();
builder.Services.AddScoped<IContentProcessor, SourceItemNormalizer>();
builder.Services.AddScoped<IDeduplicationService, DeduplicationService>();
builder.Services.AddScoped<IProcessingLogRepository, EfProcessingLogRepository>();
builder.Services.AddScoped<IClassificationStep, ClassificationStep>();

// --- Articles ---
builder.Services.AddScoped<IArticleRepository, EfArticleRepository>();
builder.Services.AddScoped<IArticleGenerationStep, ArticleGenerationStep>();

// --- AI Generation (Gemini) ---
builder.Services.Configure<GeminiSettings>(settings =>
{
    settings.ApiKey = builder.Configuration["GEMINI_API_KEY"] ?? "";
    settings.ClassificationModel = builder.Configuration["GEMINI_CLASSIFICATION_MODEL"] ?? "gemini-2.5-flash-lite";
    settings.GenerationModel = builder.Configuration["GEMINI_GENERATION_MODEL"] ?? "gemini-2.5-flash";
    if (int.TryParse(builder.Configuration["GEMINI_TIMEOUT_SECONDS"], out var timeout))
        settings.TimeoutSeconds = timeout;
});

var geminiTimeout = int.TryParse(builder.Configuration["GEMINI_TIMEOUT_SECONDS"], out var gt) ? gt : 60;
builder.Services.AddHttpClient<ITextGenerationProvider, GeminiTextGenerationProvider>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(geminiTimeout);
});
builder.Services.AddScoped<IAiClassifier, GeminiClassifier>();
builder.Services.AddScoped<IAiArticleGenerator, GeminiArticleGenerator>();

// --- CORS ---
var corsOrigin = builder.Configuration["CORS_ORIGIN"] ?? "http://localhost:3000";
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// --- Migrations e seed automáticos (controlado por RUN_MIGRATIONS_ON_STARTUP) ---
if (!app.Environment.IsEnvironment("Testing") &&
    app.Configuration.GetValue<bool>("RUN_MIGRATIONS_ON_STARTUP"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    await SourceSeeder.SeedAsync(db);
}

app.UseCors();

// --- Health check ---
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// --- Controller de teste ---
app.MapGet("/api/ping", () => Results.Ok(new { message = "pong", timestamp = DateTime.UtcNow }));

app.Run();

// Necessário para WebApplicationFactory nos testes de integração
public partial class Program { }
