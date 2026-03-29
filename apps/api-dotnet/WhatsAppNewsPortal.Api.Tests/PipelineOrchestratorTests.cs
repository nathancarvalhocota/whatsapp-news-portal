using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WhatsAppNewsPortal.Api.AiGeneration.Application;
using WhatsAppNewsPortal.Api.Articles.Application;
using WhatsAppNewsPortal.Api.Common;
using WhatsAppNewsPortal.Api.ContentProcessing.Application;
using WhatsAppNewsPortal.Api.Infrastructure.Data;
using WhatsAppNewsPortal.Api.Ingestion.Application;
using WhatsAppNewsPortal.Api.Pipeline.Application;
using WhatsAppNewsPortal.Api.Pipeline.Infrastructure;
using WhatsAppNewsPortal.Api.Sources.Application;
using WhatsAppNewsPortal.Api.Sources.Domain;
using WhatsAppNewsPortal.Api.Sources.Infrastructure;
using WhatsAppNewsPortal.Api.ContentProcessing.Infrastructure;
using WhatsAppNewsPortal.Api.Articles.Infrastructure;
using WhatsAppNewsPortal.Api.Ingestion.Infrastructure;

namespace WhatsAppNewsPortal.Api.Tests;

public class PipelineOrchestratorTests : IDisposable
{
    private readonly AppDbContext _db;

    public PipelineOrchestratorTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
    }

    public void Dispose() => _db.Dispose();

    private Source CreateSource(string name = "Test Source", SourceType type = SourceType.Official, string? feedUrl = "https://test.com/rss")
    {
        var source = new Source
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            BaseUrl = $"https://{name.ToLower().Replace(" ", "")}.com",
            FeedUrl = feedUrl,
            IsActive = true
        };
        _db.Sources.Add(source);
        _db.SaveChanges();
        return source;
    }

    private static readonly LoggerFactory Lf = new();

    private PipelineOrchestrator BuildOrchestrator(
        IIngestionAdapter? rssAdapter = null,
        HtmlIngestionAdapter? htmlAdapter = null,
        IAiClassifier? classifier = null,
        IAiArticleGenerator? articleGenerator = null)
    {
        var sourceRepo = new EfSourceRepository(_db);
        var sourceItemRepo = new EfSourceItemRepository(_db);
        var contentProcessor = new SourceItemNormalizer(sourceItemRepo, Lf.CreateLogger<SourceItemNormalizer>());
        var articleRepo = new EfArticleRepository(_db);
        var logRepo = new EfProcessingLogRepository(_db);

        var classificationStep = new ClassificationStep(
            classifier ?? new FakeClassifier(),
            sourceItemRepo,
            logRepo,
            Lf.CreateLogger<ClassificationStep>());

        var articleGenStep = new ArticleGenerationStep(
            articleGenerator ?? new FakeArticleGenerator(),
            articleRepo,
            sourceItemRepo,
            logRepo,
            Lf.CreateLogger<ArticleGenerationStep>());

        var jobSettings = new PipelineJobSettings
        {
            MinPublishedDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        return new PipelineOrchestrator(
            sourceRepo,
            rssAdapter ?? new FakeIngestionAdapter([]),
            htmlAdapter ?? CreateFakeHtmlAdapter(),
            sourceItemRepo,
            contentProcessor,
            classificationStep,
            articleGenStep,
            logRepo,
            jobSettings,
            Lf.CreateLogger<PipelineOrchestrator>());
    }

    private static HtmlIngestionAdapter CreateFakeHtmlAdapter()
    {
        var fetcher = new HtmlFetcher(new HttpClient(new FakeHttpHandler()), Lf.CreateLogger<HtmlFetcher>());
        return new HtmlIngestionAdapter(fetcher, Lf.CreateLogger<HtmlIngestionAdapter>());
    }

    [Fact]
    public async Task RunAsync_NoSources_ReturnsEmptyResult()
    {
        var orchestrator = BuildOrchestrator();

        var result = await orchestrator.RunAsync();

        Assert.Equal(0, result.SourcesProcessed);
        Assert.Equal(0, result.ItemsDiscovered);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public async Task RunAsync_WithItems_FullPipelineProducesDrafts()
    {
        var source = CreateSource();
        var items = new List<DiscoveredItemDto>
        {
            new()
            {
                SourceId = source.Id,
                OriginalUrl = "https://blog.whatsapp.com/new-feature-1",
                Title = "New Feature Announcement",
                RawContent = "<p>WhatsApp is launching a new feature for screen sharing in video calls. This allows users to share their screens during group video calls with up to 32 people.</p>"
            }
        };

        var orchestrator = BuildOrchestrator(rssAdapter: new FakeIngestionAdapter(items));

        var result = await orchestrator.RunAsync();

        Assert.Equal(1, result.SourcesProcessed);
        Assert.Equal(1, result.ItemsDiscovered);
        Assert.Equal(1, result.ItemsNormalized);
        Assert.Equal(1, result.ItemsClassified);
        Assert.Equal(1, result.DraftsGenerated);
        Assert.False(result.HasErrors);

        // Verify article was persisted
        var article = await _db.Articles.FirstOrDefaultAsync();
        Assert.NotNull(article);
        Assert.Equal(PipelineStatus.Draft, article.Status);

        // Verify item summary
        Assert.Single(result.Items);
        Assert.Equal("draft", result.Items[0].Status);
        Assert.NotNull(result.Items[0].ArticleId);
    }

    [Fact]
    public async Task RunAsync_DuplicateUrl_SkipsItem()
    {
        var source = CreateSource();
        var url = "https://blog.whatsapp.com/existing-post";

        // Pre-insert a SourceItem with the same URL
        _db.SourceItems.Add(new SourceItem
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            OriginalUrl = url,
            Title = "Existing",
            Status = PipelineStatus.Draft
        });
        await _db.SaveChangesAsync();

        var items = new List<DiscoveredItemDto>
        {
            new() { SourceId = source.Id, OriginalUrl = url, Title = "Duplicate" }
        };

        var orchestrator = BuildOrchestrator(rssAdapter: new FakeIngestionAdapter(items));

        var result = await orchestrator.RunAsync();

        Assert.Equal(1, result.ItemsDiscovered);
        Assert.Equal(1, result.ItemsDeduplicated);
        Assert.Equal(0, result.DraftsGenerated);
        Assert.Equal("skipped_duplicate_url", result.Items[0].Status);
    }

    [Fact]
    public async Task RunAsync_ClassificationFailure_DoesNotStopOtherItems()
    {
        var source = CreateSource();
        var items = new List<DiscoveredItemDto>
        {
            new()
            {
                SourceId = source.Id,
                OriginalUrl = "https://blog.whatsapp.com/fail-item",
                Title = "Will Fail Classification",
                RawContent = "<p>This item will fail classification because the classifier throws. Content is long enough for normalization to pass validation.</p>"
            },
            new()
            {
                SourceId = source.Id,
                OriginalUrl = "https://blog.whatsapp.com/success-item",
                Title = "Will Succeed",
                RawContent = "<p>WhatsApp is launching a new feature for screen sharing in video calls. This allows users to share their screens during group video calls with up to 32 people.</p>"
            }
        };

        var failingClassifier = new FailOnFirstClassifier();
        var orchestrator = BuildOrchestrator(
            rssAdapter: new FakeIngestionAdapter(items),
            classifier: failingClassifier);

        var result = await orchestrator.RunAsync();

        Assert.Equal(2, result.ItemsDiscovered);
        Assert.Equal(2, result.ItemsNormalized);
        // At least one should succeed
        Assert.True(result.HasErrors);
        Assert.True(result.DraftsGenerated >= 1);
    }

    [Fact]
    public async Task RunAsync_MultipleItems_ProducesCorrectItemSummaries()
    {
        var source = CreateSource();
        var items = new List<DiscoveredItemDto>
        {
            new()
            {
                SourceId = source.Id,
                OriginalUrl = "https://blog.whatsapp.com/feature-a",
                Title = "Feature A",
                RawContent = "<p>WhatsApp is launching feature A for screen sharing in video calls. This allows users to share their screens during group calls with up to 32 people.</p>"
            },
            new()
            {
                SourceId = source.Id,
                OriginalUrl = "https://blog.whatsapp.com/feature-b",
                Title = "Feature B",
                RawContent = "<p>WhatsApp is launching feature B for message reactions. Users can now react to messages with any emoji in chats and groups with many different reactions.</p>"
            }
        };

        var orchestrator = BuildOrchestrator(rssAdapter: new FakeIngestionAdapter(items));

        var result = await orchestrator.RunAsync();

        Assert.Equal(2, result.DraftsGenerated);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, i => Assert.Equal("draft", i.Status));
    }

    [Fact]
    public async Task RunAsync_SourceWithNoFeed_UsesHtmlAdapter()
    {
        // Source without FeedUrl => should use HtmlIngestionAdapter
        var source = CreateSource(name: "No Feed Source", feedUrl: null);

        var orchestrator = BuildOrchestrator();

        var result = await orchestrator.RunAsync();

        // HtmlIngestionAdapter should be called but return empty (fake handler)
        Assert.Equal(1, result.SourcesProcessed);
        Assert.Equal(0, result.ItemsDiscovered);
    }

    [Fact]
    public async Task RunAsync_IngestionFailure_ContinuesToNextSource()
    {
        var source1 = CreateSource(name: "Failing Source");
        var source2 = CreateSource(name: "Working Source");

        var failingAdapter = new FailingIngestionAdapter(source1.Id);
        var items2 = new List<DiscoveredItemDto>
        {
            new()
            {
                SourceId = source2.Id,
                OriginalUrl = "https://workingsource.com/post",
                Title = "Working Post",
                RawContent = "<p>WhatsApp is launching a new feature for video calls. This allows users to share their screens during group video calls with up to 32 people.</p>"
            }
        };

        // The failingAdapter will throw for source1 but return items for source2
        var combinedAdapter = new SelectiveIngestionAdapter(
            new Dictionary<Guid, List<DiscoveredItemDto>> { [source2.Id] = items2 },
            failSourceIds: [source1.Id]);

        var orchestrator = BuildOrchestrator(rssAdapter: combinedAdapter);

        var result = await orchestrator.RunAsync();

        Assert.Equal(2, result.SourcesProcessed);
        Assert.True(result.HasErrors);
        Assert.Equal(1, result.DraftsGenerated); // source2 items processed
    }

    [Fact]
    public async Task RunAsync_ProcessingLogs_AreCreated()
    {
        var source = CreateSource();
        var items = new List<DiscoveredItemDto>
        {
            new()
            {
                SourceId = source.Id,
                OriginalUrl = "https://blog.whatsapp.com/log-test",
                Title = "Log Test",
                RawContent = "<p>WhatsApp is launching a new feature for screen sharing in video calls. This allows users to share their screens during group calls with up to 32 people.</p>"
            }
        };

        var orchestrator = BuildOrchestrator(rssAdapter: new FakeIngestionAdapter(items));

        await orchestrator.RunAsync();

        var logs = await _db.ProcessingLogs.ToListAsync();
        Assert.NotEmpty(logs);
        // Should have logs for ingestion, normalization, deduplication steps
        Assert.Contains(logs, l => l.StepName == "ingestion");
        Assert.Contains(logs, l => l.StepName == "normalization");
        Assert.Contains(logs, l => l.StepName == "deduplication");
    }

    [Fact]
    public async Task RunAsync_Timestamps_AreSet()
    {
        var orchestrator = BuildOrchestrator();

        var result = await orchestrator.RunAsync();

        Assert.True(result.StartedAt <= result.FinishedAt);
        Assert.True(result.FinishedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task RunAsync_ArticleGenerationFailure_DoesNotStopOtherItems()
    {
        // A falha na geração de artigo de um item não deve impedir o processamento dos demais.
        var source = CreateSource();
        var items = new List<DiscoveredItemDto>
        {
            new()
            {
                SourceId = source.Id,
                OriginalUrl = "https://blog.whatsapp.com/gen-fail-item",
                Title = "Will Fail Generation",
                RawContent = "<p>WhatsApp is launching a new feature for screen sharing in video calls. This allows users to share their screens during group video calls with up to 32 people.</p>"
            },
            new()
            {
                SourceId = source.Id,
                OriginalUrl = "https://blog.whatsapp.com/gen-success-item",
                Title = "Will Succeed Generation",
                RawContent = "<p>WhatsApp is launching a new feature for message reactions. Users can now react to any message with any emoji in groups and chats everywhere.</p>"
            }
        };

        var failingGenerator = new FailOnFirstGenerator();
        var orchestrator = BuildOrchestrator(
            rssAdapter: new FakeIngestionAdapter(items),
            articleGenerator: failingGenerator);

        var result = await orchestrator.RunAsync();

        Assert.Equal(2, result.ItemsDiscovered);
        Assert.True(result.HasErrors);
        Assert.True(result.DraftsGenerated >= 1);
    }

    [Fact]
    public async Task RunAsync_ClassificationError_SourceItemIsDeletedAndLogIsKept()
    {
        // Quando a classificação falha, o SourceItem deve ser removido do banco
        // para permitir reprocessamento, mas o ProcessingLog deve ser mantido.
        var source = CreateSource();
        var url = "https://blog.whatsapp.com/classify-fail-item";
        var items = new List<DiscoveredItemDto>
        {
            new()
            {
                SourceId = source.Id,
                OriginalUrl = url,
                Title = "Item That Fails Classification",
                RawContent = "<p>WhatsApp is launching a new feature for screen sharing in video calls. This allows users to share their screens during group video calls with up to 32 people.</p>"
            }
        };

        var orchestrator = BuildOrchestrator(
            rssAdapter: new FakeIngestionAdapter(items),
            classifier: new AlwaysFailingClassifier());

        var result = await orchestrator.RunAsync();

        Assert.True(result.HasErrors);
        var sourceItem = await _db.SourceItems.FirstOrDefaultAsync(si => si.OriginalUrl == url);
        Assert.Null(sourceItem);

        // ProcessingLog deve existir para auditoria
        var logs = await _db.ProcessingLogs.ToListAsync();
        Assert.Contains(logs, l => l.StepName == "classification" && l.Status == "failed");
    }

    [Fact]
    public async Task RunAsync_ArticleGenerationError_SourceItemIsDeletedAndLogIsKept()
    {
        // Quando a geração falha, o SourceItem deve ser removido do banco
        // para permitir reprocessamento, mas o ProcessingLog deve ser mantido.
        var source = CreateSource();
        var url = "https://blog.whatsapp.com/gen-fail-persist";
        var items = new List<DiscoveredItemDto>
        {
            new()
            {
                SourceId = source.Id,
                OriginalUrl = url,
                Title = "Item That Fails Generation",
                RawContent = "<p>WhatsApp is launching a new feature for screen sharing in video calls. This allows users to share their screens during group video calls with up to 32 people.</p>"
            }
        };

        var orchestrator = BuildOrchestrator(
            rssAdapter: new FakeIngestionAdapter(items),
            articleGenerator: new AlwaysFailingGenerator());

        var result = await orchestrator.RunAsync();

        Assert.True(result.HasErrors);
        var sourceItem = await _db.SourceItems.FirstOrDefaultAsync(si => si.OriginalUrl == url);
        Assert.Null(sourceItem);

        // ProcessingLog deve existir para auditoria
        var logs = await _db.ProcessingLogs.ToListAsync();
        Assert.Contains(logs, l => l.StepName == "article_generation" && l.Status == "failed");
    }

    [Fact]
    public async Task RunAsync_MultipleSourcesOneFailsIngestion_OtherSourceStillProcessed()
    {
        // Falha de ingestão em uma fonte HTTP não deve impedir outras fontes.
        var failSource = CreateSource(name: "Feed Indisponível");
        var goodSource = CreateSource(name: "Feed OK");
        var items = new List<DiscoveredItemDto>
        {
            new()
            {
                SourceId = goodSource.Id,
                OriginalUrl = "https://feed-ok.com/post-1",
                Title = "Post from working source",
                RawContent = "<p>WhatsApp is launching a new feature for screen sharing. Users can share their screens during group video calls with up to 32 people.</p>"
            }
        };

        var adapter = new SelectiveIngestionAdapter(
            new Dictionary<Guid, List<DiscoveredItemDto>> { [goodSource.Id] = items },
            failSourceIds: [failSource.Id]);

        var orchestrator = BuildOrchestrator(rssAdapter: adapter);
        var result = await orchestrator.RunAsync();

        Assert.Equal(2, result.SourcesProcessed);
        Assert.True(result.HasErrors);
        Assert.Equal(1, result.DraftsGenerated);
    }

    // --- Fakes ---

    private class FakeIngestionAdapter(List<DiscoveredItemDto> items) : IIngestionAdapter
    {
        public Task<List<DiscoveredItemDto>> FetchItemsAsync(Source source, CancellationToken ct = default)
            => Task.FromResult(items);
    }

    private class FailingIngestionAdapter(Guid failSourceId) : IIngestionAdapter
    {
        public Task<List<DiscoveredItemDto>> FetchItemsAsync(Source source, CancellationToken ct = default)
        {
            if (source.Id == failSourceId)
                throw new HttpRequestException("Feed indisponível");
            return Task.FromResult(new List<DiscoveredItemDto>());
        }
    }

    private class SelectiveIngestionAdapter(
        Dictionary<Guid, List<DiscoveredItemDto>> itemsBySource,
        HashSet<Guid> failSourceIds) : IIngestionAdapter
    {
        public Task<List<DiscoveredItemDto>> FetchItemsAsync(Source source, CancellationToken ct = default)
        {
            if (failSourceIds.Contains(source.Id))
                throw new HttpRequestException("Feed indisponível");
            return Task.FromResult(
                itemsBySource.TryGetValue(source.Id, out var items) ? items : []);
        }
    }

    private class FakeClassifier : IAiClassifier
    {
        public Task<ClassificationResultDto> ClassifyAsync(NormalizedItemDto item, CancellationToken ct = default)
        {
            return Task.FromResult(new ClassificationResultDto
            {
                IsRelevant = true,
                EditorialType = EditorialType.OfficialNews,
                SuggestedTitle = "WhatsApp lança novo recurso",
                Slug = $"whatsapp-lanca-novo-recurso-{Guid.NewGuid().ToString("N")[..6]}",
                MetaTitle = "WhatsApp lança novo recurso | Portal",
                MetaDescription = "Descubra o novo recurso do WhatsApp.",
                Excerpt = "O WhatsApp anunciou um novo recurso para seus usuários.",
                Tags = ["whatsapp", "novidade"],
                EditorialNote = null
            });
        }
    }

    private class FailOnFirstClassifier : IAiClassifier
    {
        private int _callCount;

        public Task<ClassificationResultDto> ClassifyAsync(NormalizedItemDto item, CancellationToken ct = default)
        {
            _callCount++;
            if (_callCount == 1)
                throw new Exception("AI classification failed");

            return Task.FromResult(new ClassificationResultDto
            {
                IsRelevant = true,
                EditorialType = EditorialType.OfficialNews,
                SuggestedTitle = "WhatsApp recurso",
                Slug = $"whatsapp-recurso-{Guid.NewGuid().ToString("N")[..6]}",
                MetaTitle = "WhatsApp recurso | Portal",
                MetaDescription = "Descubra o novo recurso.",
                Excerpt = "Novo recurso anunciado.",
                Tags = ["whatsapp"],
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
                ContentHtml = "<h2>Detalhes</h2><p>O WhatsApp anunciou um novo recurso para seus usuários. A novidade já está disponível para todos.</p>",
                BetaDisclaimer = null
            });
        }
    }

    private class FakeHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("<html><body></body></html>")
            });
        }
    }

    private class FailOnFirstGenerator : IAiArticleGenerator
    {
        private int _callCount;

        public Task<GeneratedArticleDto> GenerateArticleAsync(
            NormalizedItemDto item, ClassificationResultDto classification, CancellationToken ct = default)
        {
            _callCount++;
            if (_callCount == 1)
                throw new Exception("AI article generation failed");

            return Task.FromResult(new GeneratedArticleDto
            {
                Title = classification.SuggestedTitle,
                Excerpt = classification.Excerpt,
                ContentHtml = "<h2>Detalhes</h2><p>O WhatsApp anunciou um novo recurso para seus usuários. A novidade já está disponível.</p>",
                BetaDisclaimer = null
            });
        }
    }

    private class AlwaysFailingClassifier : IAiClassifier
    {
        public Task<ClassificationResultDto> ClassifyAsync(
            NormalizedItemDto item, CancellationToken ct = default)
            => throw new Exception("AI classification service unavailable");
    }

    private class AlwaysFailingGenerator : IAiArticleGenerator
    {
        public Task<GeneratedArticleDto> GenerateArticleAsync(
            NormalizedItemDto item, ClassificationResultDto classification, CancellationToken ct = default)
            => throw new Exception("AI generation service unavailable");
    }
}
