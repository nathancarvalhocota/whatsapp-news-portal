using Microsoft.EntityFrameworkCore;
using WhatsAppNewsPortal.Api.AiGeneration.Application;
using WhatsAppNewsPortal.Api.AiGeneration.Infrastructure;
using WhatsAppNewsPortal.Api.Articles.Application;
using WhatsAppNewsPortal.Api.Articles.Domain;
using WhatsAppNewsPortal.Api.Articles.Infrastructure;
using WhatsAppNewsPortal.Api.Common;
using WhatsAppNewsPortal.Api.Infrastructure.Data;
using WhatsAppNewsPortal.Api.ContentProcessing.Application;
using WhatsAppNewsPortal.Api.ContentProcessing.Infrastructure;
using WhatsAppNewsPortal.Api.Ingestion.Application;
using WhatsAppNewsPortal.Api.Ingestion.Infrastructure;
using WhatsAppNewsPortal.Api.Sources.Application;
using WhatsAppNewsPortal.Api.Sources.Domain;
using WhatsAppNewsPortal.Api.Pipeline.Application;
using WhatsAppNewsPortal.Api.Pipeline.Infrastructure;
using WhatsAppNewsPortal.Api.Demo.Application;
using WhatsAppNewsPortal.Api.Demo.Infrastructure;
using WhatsAppNewsPortal.Api.Sources.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// --- Logging estruturado ---
builder.Logging.ClearProviders();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddSimpleConsole(options =>
    {
        options.IncludeScopes = true;
        options.TimestampFormat = "HH:mm:ss ";
    });
}
else
{
    builder.Logging.AddJsonConsole(options =>
    {
        options.IncludeScopes = true;
    });
}

// --- PostgreSQL + EF Core ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
    connectionString = builder.Configuration["DATABASE_URL"];
if (string.IsNullOrEmpty(connectionString))
    throw new InvalidOperationException("Connection string not configured. Set ConnectionStrings__DefaultConnection or DATABASE_URL.");

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

// --- Sources ---
builder.Services.AddScoped<ISourceRepository, EfSourceRepository>();

// --- ContentProcessing ---
builder.Services.AddScoped<ISourceItemRepository, EfSourceItemRepository>();
builder.Services.AddScoped<IContentProcessor, SourceItemNormalizer>();
builder.Services.AddScoped<IDeduplicationService, DeduplicationService>();
builder.Services.AddScoped<IProcessingLogRepository, EfProcessingLogRepository>();
builder.Services.AddScoped<IClassificationStep, ClassificationStep>();

// --- Articles ---
builder.Services.AddScoped<IArticleRepository, EfArticleRepository>();
builder.Services.AddScoped<IArticleGenerationStep, ArticleGenerationStep>();
builder.Services.AddScoped<IArticlePublisher, ArticlePublisher>();

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

// --- Pipeline ---
// ============================================================
// CONFIGURAÇÃO DO BACKGROUND JOB — MODIFIQUE AQUI, VIA ENV VARS OU VIA /api/settings/pipeline
// Registrado como singleton mutável: alterações via API refletem no próximo ciclo do job.
// ============================================================
var pipelineSettings = new PipelineJobSettings();
{
    var defaultInterval = builder.Environment.IsDevelopment() ? 5 : 720;
    pipelineSettings.IntervalMinutes = int.TryParse(builder.Configuration["PIPELINE_INTERVAL_MINUTES"], out var interval)
        ? interval : defaultInterval;

    pipelineSettings.RunOnStartup = !bool.TryParse(builder.Configuration["PIPELINE_RUN_ON_STARTUP"], out var runOnStartup)
        || runOnStartup;

    pipelineSettings.MinPublishedDate = DateTime.TryParse(builder.Configuration["PIPELINE_MIN_DATE"], out var minDate)
        ? DateTime.SpecifyKind(minDate, DateTimeKind.Utc)
        : new DateTime(2026, 3, 28, 0, 0, 0, DateTimeKind.Utc);

    pipelineSettings.AutoPublishDrafts = !bool.TryParse(builder.Configuration["PIPELINE_AUTO_PUBLISH"], out var autoPublish)
        || autoPublish;
}
builder.Services.AddSingleton(pipelineSettings);
builder.Services.AddScoped<IPipelineOrchestrator, PipelineOrchestrator>();
builder.Services.AddHostedService<ContentPipelineJob>();

// --- Demo ---
builder.Services.AddScoped<IDemoPipelineService, DemoPipelineService>();

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
app.MapGet("/health", (IWebHostEnvironment env) => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    environment = env.EnvironmentName
}));

// --- Controller de teste ---
app.MapGet("/api/ping", () => Results.Ok(new { message = "pong", timestamp = DateTime.UtcNow }));

// --- Articles (public read endpoints) ---
app.MapGet("/api/articles/published", async (
    IArticleRepository repo,
    int page = 1,
    int pageSize = 20,
    CancellationToken ct = default) =>
{
    if (page < 1) page = 1;
    if (pageSize < 1 || pageSize > 100) pageSize = 20;
    var articles = await repo.GetPublishedAsync(page, pageSize, ct);
    return Results.Ok(articles.Select(ArticleSummaryDto.FromArticle));
});

app.MapGet("/api/articles/{slug}", async (
    string slug,
    IArticleRepository repo,
    CancellationToken ct) =>
{
    var article = await repo.GetBySlugAsync(slug, ct);
    if (article is null || article.Status != PipelineStatus.Published)
        return Results.NotFound();
    return Results.Ok(ArticleDetailDto.FromArticle(article));
});

app.MapGet("/api/categories/{category}", async (
    string category,
    IArticleRepository repo,
    int page = 1,
    int pageSize = 20,
    CancellationToken ct = default) =>
{
    if (page < 1) page = 1;
    if (pageSize < 1 || pageSize > 100) pageSize = 20;
    var articles = await repo.GetByCategoryAsync(category, page, pageSize, ct);
    return Results.Ok(articles.Select(ArticleSummaryDto.FromArticle));
});

app.MapGet("/api/sources", async (ISourceRepository repo, CancellationToken ct) =>
{
    var sources = await repo.GetActiveSourcesAsync(ct);
    return Results.Ok(sources.Select(SourceDto.FromSource));
});

// --- Articles: list drafts (admin) ---
app.MapGet("/api/articles/drafts", async (AppDbContext db, CancellationToken ct) =>
{
    var drafts = await db.Articles
        .Where(a => a.Status == PipelineStatus.Draft)
        .OrderByDescending(a => a.CreatedAt)
        .Select(a => new { a.Id, a.Slug, a.Title, a.ArticleType, a.Category, a.CreatedAt })
        .ToListAsync(ct);
    return Results.Ok(drafts);
});

// --- Articles (admin actions) ---
app.MapPost("/api/articles/{id:guid}/publish", async (Guid id, IArticlePublisher publisher, CancellationToken ct) =>
{
    try
    {
        var result = await publisher.PublishAsync(id, ct);
        return Results.Ok(result);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// --- Source items: list failed (admin) ---
app.MapGet("/api/source-items/failed", async (AppDbContext db, CancellationToken ct) =>
{
    var failed = await db.SourceItems
        .Where(si => si.Status == PipelineStatus.Failed)
        .OrderByDescending(si => si.UpdatedAt)
        .Take(50)
        .Select(si => new { si.Id, si.Title, si.OriginalUrl, si.ErrorMessage, si.UpdatedAt, si.SourceId })
        .ToListAsync(ct);
    return Results.Ok(failed);
});

// --- Source items: reprocess failed item (admin) ---
app.MapPost("/api/source-items/{id:guid}/reprocess", async (
    Guid id,
    ISourceItemRepository sourceItemRepo,
    IContentProcessor contentProcessor,
    IClassificationStep classificationStep,
    IArticleGenerationStep articleGenerationStep,
    CancellationToken ct) =>
{
    var sourceItem = await sourceItemRepo.GetByIdAsync(id, ct);
    if (sourceItem is null)
        return Results.NotFound(new { error = "Item não encontrado." });

    if (sourceItem.Status != PipelineStatus.Failed)
        return Results.BadRequest(new { error = "Apenas itens com status 'Failed' podem ser reprocessados." });

    // Reseta para reprocessamento
    sourceItem.Status = PipelineStatus.Discovered;
    sourceItem.ErrorMessage = null;
    sourceItem.UpdatedAt = DateTime.UtcNow;
    await sourceItemRepo.UpdateAsync(sourceItem, ct);

    // Normalização
    NormalizedItemDto normalized;
    try
    {
        normalized = await contentProcessor.NormalizeAsync(sourceItem, ct);
    }
    catch (Exception ex)
    {
        return Results.UnprocessableEntity(new { error = $"Falha na normalização: {ex.Message}" });
    }

    if (string.IsNullOrEmpty(normalized.NormalizedContent))
        return Results.UnprocessableEntity(new { error = "Falha na normalização: conteúdo vazio após limpeza." });

    // Classificação
    var classResult = await classificationStep.ExecuteAsync(normalized, ct);
    if (!classResult.Success)
        return Results.UnprocessableEntity(new { error = $"Falha na classificação: {classResult.ErrorMessage}" });

    if (!classResult.IsRelevant)
        return Results.Ok(new { status = "discarded", reason = classResult.DiscardReason });

    // Geração de artigo
    var genResult = await articleGenerationStep.ExecuteAsync(normalized, classResult.Classification!, ct);
    if (!genResult.Success)
        return Results.UnprocessableEntity(new { error = $"Falha na geração: {genResult.ErrorMessage}" });

    return Results.Ok(new
    {
        status = "draft",
        articleId = genResult.ArticleId,
        slug = genResult.Slug
    });
});

// --- Pipeline: run full pipeline ---
app.MapPost("/api/pipeline/run", async (IPipelineOrchestrator orchestrator, CancellationToken ct) =>
{
    var result = await orchestrator.RunAsync(ct);
    return Results.Ok(result);
});

// --- Pipeline settings (admin) ---
app.MapGet("/api/settings/pipeline", (PipelineJobSettings settings) =>
{
    return Results.Ok(new
    {
        intervalMinutes = settings.IntervalMinutes,
        minPublishedDate = settings.MinPublishedDate.ToString("yyyy-MM-dd"),
        autoPublishDrafts = settings.AutoPublishDrafts
    });
});

app.MapPut("/api/settings/pipeline", (PipelineSettingsUpdateRequest req, PipelineJobSettings settings, ILogger<Program> logger) =>
{
    var prev = new
    {
        settings.IntervalMinutes,
        MinPublishedDate = settings.MinPublishedDate.ToString("yyyy-MM-dd"),
        settings.AutoPublishDrafts
    };

    if (req.IntervalMinutes.HasValue)
    {
        if (req.IntervalMinutes.Value < 1) return Results.BadRequest(new { error = "intervalMinutes deve ser >= 1" });
        settings.IntervalMinutes = req.IntervalMinutes.Value;
    }
    if (req.MinPublishedDate is not null)
    {
        if (!DateTime.TryParse(req.MinPublishedDate, out var parsed))
            return Results.BadRequest(new { error = "minPublishedDate inválida (use yyyy-MM-dd)" });
        settings.MinPublishedDate = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
    }
    if (req.AutoPublishDrafts.HasValue)
        settings.AutoPublishDrafts = req.AutoPublishDrafts.Value;

    logger.LogInformation(
        "[Settings] Pipeline settings atualizadas via admin — " +
        "Intervalo: {PrevInterval}min → {NewInterval}min | " +
        "MinDate: {PrevDate} → {NewDate} | " +
        "AutoPublish: {PrevAuto} → {NewAuto}",
        prev.IntervalMinutes, settings.IntervalMinutes,
        prev.MinPublishedDate, settings.MinPublishedDate.ToString("yyyy-MM-dd"),
        prev.AutoPublishDrafts, settings.AutoPublishDrafts);

    // Interrompe o delay do job para que ele reinicie o ciclo com os novos valores
    settings.InterruptDelay();

    return Results.Ok(new
    {
        intervalMinutes = settings.IntervalMinutes,
        minPublishedDate = settings.MinPublishedDate.ToString("yyyy-MM-dd"),
        autoPublishDrafts = settings.AutoPublishDrafts
    });
});

// --- Demo pipeline ---
app.MapPost("/api/pipeline/run-demo", async (DemoPipelineRequest request, IDemoPipelineService demo, CancellationToken ct) =>
{
    var result = await demo.RunDemoAsync(request, ct);
    return result.Success ? Results.Ok(result) : Results.UnprocessableEntity(result);
});

// --- Dev: seed a draft for manual testing (only in Development) ---
if (app.Environment.IsDevelopment())
{
    app.MapPost("/api/dev/seed-draft", async (AppDbContext db) =>
    {
        // Reuse existing source or create one
        var source = await db.Sources.FirstOrDefaultAsync(s => s.Name == "WhatsApp Blog");
        if (source is null)
        {
            source = new Source
            {
                Id = Guid.NewGuid(),
                Name = "WhatsApp Blog",
                Type = SourceType.Official,
                BaseUrl = "https://blog.whatsapp.com",
                FeedUrl = "https://blog.whatsapp.com/rss.xml",
                IsActive = true
            };
            db.Sources.Add(source);
            await db.SaveChangesAsync();
        }

        var sourceItem = new SourceItem
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            OriginalUrl = $"https://blog.whatsapp.com/screen-sharing-{Guid.NewGuid().ToString("N")[..8]}",
            CanonicalUrl = "https://blog.whatsapp.com/screen-sharing-video-calls",
            Title = "Screen Sharing on Video Calls",
            NormalizedContent = "Today we're launching screen sharing on WhatsApp video calls.",
            ContentHash = Guid.NewGuid().ToString("N"),
            Status = PipelineStatus.Draft,
            SourceClassification = "official_news"
        };
        db.SourceItems.Add(sourceItem);
        await db.SaveChangesAsync();

        var articleId = Guid.NewGuid();
        var article = new Article
        {
            Id = articleId,
            SourceItemId = sourceItem.Id,
            Slug = $"whatsapp-lanca-compartilhamento-de-tela-{Guid.NewGuid().ToString("N")[..6]}",
            Title = "WhatsApp lanca compartilhamento de tela em videochamadas para todos os usuarios",
            Excerpt = "O WhatsApp anunciou oficialmente o recurso de compartilhamento de tela durante videochamadas, ja disponivel para Android e iOS.",
            ContentHtml =
                "<h2>O que e o novo recurso</h2>" +
                "<p>O WhatsApp lancou oficialmente o compartilhamento de tela durante videochamadas. " +
                "Com essa funcionalidade, os usuarios podem mostrar o conteudo de seus dispositivos " +
                "em tempo real durante uma chamada de video.</p>" +
                "<h2>Como funciona na pratica</h2>" +
                "<p>Para usar, basta iniciar uma videochamada, tocar no icone de compartilhamento " +
                "e selecionar \"Compartilhar tela\".</p>",
            MetaTitle = "WhatsApp lanca compartilhamento de tela em videochamadas",
            MetaDescription = "O WhatsApp lancou oficialmente o compartilhamento de tela durante videochamadas.",
            Tags = ["whatsapp", "videochamada", "compartilhamento-de-tela"],
            ArticleType = EditorialType.OfficialNews,
            Category = "oficial",
            Status = PipelineStatus.Draft,
            PublishedAt = null
        };
        db.Articles.Add(article);

        db.ArticleSourceReferences.Add(new ArticleSourceReference
        {
            Id = Guid.NewGuid(),
            ArticleId = articleId,
            SourceName = "WhatsApp Blog",
            SourceUrl = sourceItem.OriginalUrl,
            ReferenceType = "primary"
        });

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            message = "Draft seeded successfully. Now call POST /api/articles/{articleId}/publish",
            articleId = article.Id,
            slug = article.Slug,
            status = article.Status.ToString()
        });
    });
}

app.Run();

// Necessário para WebApplicationFactory nos testes de integração
public partial class Program { }

public class PipelineSettingsUpdateRequest
{
    public int? IntervalMinutes { get; set; }
    public string? MinPublishedDate { get; set; }
    public bool? AutoPublishDrafts { get; set; }
}
