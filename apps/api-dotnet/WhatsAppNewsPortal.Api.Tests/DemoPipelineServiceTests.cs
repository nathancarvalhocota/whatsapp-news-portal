using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WhatsAppNewsPortal.Api.AiGeneration.Application;
using WhatsAppNewsPortal.Api.Articles.Application;
using WhatsAppNewsPortal.Api.Articles.Infrastructure;
using WhatsAppNewsPortal.Api.Common;
using WhatsAppNewsPortal.Api.ContentProcessing.Application;
using WhatsAppNewsPortal.Api.ContentProcessing.Infrastructure;
using WhatsAppNewsPortal.Api.Demo.Application;
using WhatsAppNewsPortal.Api.Demo.Infrastructure;
using WhatsAppNewsPortal.Api.Infrastructure.Data;
using WhatsAppNewsPortal.Api.Ingestion.Application;
using WhatsAppNewsPortal.Api.Sources.Application;
using WhatsAppNewsPortal.Api.Sources.Domain;
using WhatsAppNewsPortal.Api.Sources.Infrastructure;

namespace WhatsAppNewsPortal.Api.Tests;

public class DemoPipelineServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private static readonly LoggerFactory Lf = new();

    public DemoPipelineServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
    }

    public void Dispose() => _db.Dispose();

    private Source CreateSource(
        string name = "WhatsApp Blog",
        SourceType type = SourceType.Official,
        string baseUrl = "https://blog.whatsapp.com",
        string? feedUrl = "https://blog.whatsapp.com/rss.xml")
    {
        var source = new Source
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            BaseUrl = baseUrl,
            FeedUrl = feedUrl,
            IsActive = true
        };
        _db.Sources.Add(source);
        _db.SaveChanges();
        return source;
    }

    private DemoPipelineService BuildService(
        IHtmlFetcher? htmlFetcher = null,
        IAiClassifier? classifier = null,
        IAiArticleGenerator? articleGenerator = null)
    {
        var sourceRepo = new EfSourceRepository(_db);
        var sourceItemRepo = new EfSourceItemRepository(_db);
        var logRepo = new EfProcessingLogRepository(_db);
        var contentProcessor = new SourceItemNormalizer(sourceItemRepo, Lf.CreateLogger<SourceItemNormalizer>());
        var classificationStep = new ClassificationStep(
            classifier ?? new FakeClassifier(),
            sourceItemRepo,
            logRepo,
            Lf.CreateLogger<ClassificationStep>());
        var articleGenStep = new ArticleGenerationStep(
            articleGenerator ?? new FakeArticleGenerator(),
            new EfArticleRepository(_db),
            sourceItemRepo,
            logRepo,
            Lf.CreateLogger<ArticleGenerationStep>());

        return new DemoPipelineService(
            _db,
            sourceRepo,
            sourceItemRepo,
            htmlFetcher ?? new FakeHtmlFetcher(),
            contentProcessor,
            classificationStep,
            articleGenStep,
            logRepo,
            Lf.CreateLogger<DemoPipelineService>());
    }

    // ── Core flow ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RunDemo_WithValidUrl_ProducesDraftArticle()
    {
        CreateSource();
        var service = BuildService();

        var result = await service.RunDemoAsync(new DemoPipelineRequest
        {
            Url = "https://blog.whatsapp.com/new-demo-feature"
        });

        Assert.True(result.Success);
        Assert.Equal("draft", result.Status);
        Assert.NotNull(result.ArticleId);
        Assert.NotNull(result.Slug);
        Assert.NotNull(result.SourceItemId);
        Assert.Equal("WhatsApp Blog", result.SourceName);

        // Verify IsDemoItem flag
        var sourceItem = await _db.SourceItems.FindAsync(result.SourceItemId);
        Assert.NotNull(sourceItem);
        Assert.True(sourceItem.IsDemoItem);

        // Verify article was persisted
        var article = await _db.Articles.FindAsync(result.ArticleId);
        Assert.NotNull(article);
        Assert.Equal(PipelineStatus.Draft, article.Status);
    }

    [Fact]
    public async Task RunDemo_EmptyUrl_ReturnsError()
    {
        var service = BuildService();

        var result = await service.RunDemoAsync(new DemoPipelineRequest { Url = "" });

        Assert.False(result.Success);
        Assert.Contains("URL is required", result.ErrorMessage);
    }

    [Fact]
    public async Task RunDemo_NullUrl_ReturnsError()
    {
        var service = BuildService();

        var result = await service.RunDemoAsync(new DemoPipelineRequest { Url = null });

        Assert.False(result.Success);
        Assert.Contains("URL is required", result.ErrorMessage);
    }

    // ── Idempotent re-execution ────────────────────────────────────────────

    [Fact]
    public async Task RunDemo_SameUrlTwice_WithoutReset_ReturnsExistingData()
    {
        CreateSource();
        var service = BuildService();
        var url = "https://blog.whatsapp.com/idempotent-test";

        // First execution
        var first = await service.RunDemoAsync(new DemoPipelineRequest { Url = url });
        Assert.True(first.Success);

        // Second execution without reset — should return existing data
        var second = await service.RunDemoAsync(new DemoPipelineRequest { Url = url });
        Assert.True(second.Success);
        Assert.Equal(first.SourceItemId, second.SourceItemId);
        Assert.Equal(first.ArticleId, second.ArticleId);
        Assert.Contains("already exists", second.Steps.Last());

        // Only one SourceItem should exist
        var count = await _db.SourceItems.CountAsync(si => si.OriginalUrl == url);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task RunDemo_SameUrlTwice_WithReset_ReprocessesSuccessfully()
    {
        CreateSource();
        var service = BuildService();
        var url = "https://blog.whatsapp.com/reset-test";

        // First execution
        var first = await service.RunDemoAsync(new DemoPipelineRequest { Url = url });
        Assert.True(first.Success);
        var firstArticleId = first.ArticleId;

        // Second execution with reset
        var second = await service.RunDemoAsync(new DemoPipelineRequest { Url = url, Reset = true });
        Assert.True(second.Success);
        Assert.True(second.WasReset);
        Assert.NotEqual(firstArticleId, second.ArticleId);

        // Old article should be gone
        var oldArticle = await _db.Articles.FindAsync(firstArticleId);
        Assert.Null(oldArticle);

        // Only one SourceItem with this URL should remain
        var count = await _db.SourceItems.CountAsync(si => si.OriginalUrl == url);
        Assert.Equal(1, count);
    }

    // ── Source matching ────────────────────────────────────────────────────

    [Fact]
    public async Task RunDemo_MatchesSourceByDomain()
    {
        var wabeta = CreateSource(
            name: "WABetaInfo",
            type: SourceType.BetaSpecialized,
            baseUrl: "https://wabetainfo.com",
            feedUrl: "https://wabetainfo.com/feed");

        CreateSource(); // WhatsApp Blog as fallback

        var service = BuildService();

        var result = await service.RunDemoAsync(new DemoPipelineRequest
        {
            Url = "https://wabetainfo.com/some-beta-feature"
        });

        Assert.True(result.Success);
        Assert.Equal("WABetaInfo", result.SourceName);
    }

    [Fact]
    public async Task RunDemo_UnknownDomain_ReturnsError()
    {
        CreateSource();
        var service = BuildService();

        var result = await service.RunDemoAsync(new DemoPipelineRequest
        {
            Url = "https://unknown-site.com/some-article"
        });

        Assert.False(result.Success);
        Assert.Contains("No active source", result.ErrorMessage);
    }

    // ── Error handling ─────────────────────────────────────────────────────

    [Fact]
    public async Task RunDemo_FetchFails_ReturnsError()
    {
        CreateSource();
        var service = BuildService(htmlFetcher: new FailingHtmlFetcher());

        var result = await service.RunDemoAsync(new DemoPipelineRequest
        {
            Url = "https://blog.whatsapp.com/unreachable"
        });

        Assert.False(result.Success);
        Assert.Contains("Failed to fetch", result.ErrorMessage);
    }

    [Fact]
    public async Task RunDemo_NoActiveSources_ReturnsError()
    {
        // No sources seeded
        var service = BuildService();

        var result = await service.RunDemoAsync(new DemoPipelineRequest
        {
            Url = "https://blog.whatsapp.com/no-sources"
        });

        Assert.False(result.Success);
        Assert.Contains("No active source", result.ErrorMessage);
    }

    // ── Processing logs ────────────────────────────────────────────────────

    [Fact]
    public async Task RunDemo_CreatesProcessingLogs()
    {
        CreateSource();
        var service = BuildService();

        var result = await service.RunDemoAsync(new DemoPipelineRequest
        {
            Url = "https://blog.whatsapp.com/log-demo-test"
        });

        Assert.True(result.Success);

        var logs = await _db.ProcessingLogs
            .Where(l => l.SourceItemId == result.SourceItemId)
            .ToListAsync();

        Assert.NotEmpty(logs);
        Assert.Contains(logs, l => l.StepName == "demo_ingestion");
    }

    // ── Fakes ──────────────────────────────────────────────────────────────

    private class FakeHtmlFetcher : IHtmlFetcher
    {
        public Task<string?> FetchHtmlAsync(string url, CancellationToken ct = default)
        {
            var html = """
                <html>
                <head><title>WhatsApp lança novo recurso de chamadas</title></head>
                <body>
                <article>
                <h1>WhatsApp lança novo recurso de chamadas</h1>
                <p>O WhatsApp anunciou hoje um novo recurso que permite aos usuários realizar
                chamadas de vídeo com até 32 participantes simultaneamente. A novidade já está
                disponível para Android e iOS e representa uma das maiores atualizações do
                aplicativo em 2026. O recurso inclui compartilhamento de tela e filtros de fundo.</p>
                </article>
                </body>
                </html>
                """;
            return Task.FromResult<string?>(html);
        }
    }

    private class FailingHtmlFetcher : IHtmlFetcher
    {
        public Task<string?> FetchHtmlAsync(string url, CancellationToken ct = default)
            => Task.FromResult<string?>(null);
    }

    private class FakeClassifier : IAiClassifier
    {
        public Task<ClassificationResultDto> ClassifyAsync(NormalizedItemDto item, CancellationToken ct = default)
        {
            return Task.FromResult(new ClassificationResultDto
            {
                IsRelevant = true,
                EditorialType = EditorialType.OfficialNews,
                SuggestedTitle = "WhatsApp lança novo recurso de chamadas",
                Slug = $"whatsapp-novo-recurso-chamadas-{Guid.NewGuid().ToString("N")[..6]}",
                MetaTitle = "WhatsApp lança novo recurso de chamadas | Portal",
                MetaDescription = "O WhatsApp anunciou um novo recurso de chamadas de vídeo.",
                Excerpt = "O WhatsApp anunciou um novo recurso que permite chamadas de vídeo com até 32 participantes.",
                Tags = ["whatsapp", "chamadas", "videochamada"],
                EditorialNote = null
            });
        }
    }

    private class FakeArticleGenerator : IAiArticleGenerator
    {
        public Task<GeneratedArticleDto> GenerateArticleAsync(
            NormalizedItemDto item, ClassificationResultDto classification, CancellationToken ct = default)
        {
            return Task.FromResult(new GeneratedArticleDto
            {
                Title = classification.SuggestedTitle,
                Excerpt = classification.Excerpt,
                ContentHtml = "<h2>Detalhes do recurso</h2><p>O WhatsApp anunciou um novo recurso de chamadas de vídeo com até 32 participantes.</p>",
                BetaDisclaimer = null
            });
        }
    }
}
