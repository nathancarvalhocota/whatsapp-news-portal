using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WhatsAppNewsPortal.Api.Articles.Application;
using WhatsAppNewsPortal.Api.Articles.Domain;
using WhatsAppNewsPortal.Api.Articles.Infrastructure;
using WhatsAppNewsPortal.Api.Common;
using WhatsAppNewsPortal.Api.ContentProcessing.Application;
using WhatsAppNewsPortal.Api.ContentProcessing.Infrastructure;
using WhatsAppNewsPortal.Api.Infrastructure.Data;
using WhatsAppNewsPortal.Api.Sources.Application;
using WhatsAppNewsPortal.Api.Sources.Domain;
using WhatsAppNewsPortal.Api.Sources.Infrastructure;

namespace WhatsAppNewsPortal.Api.Tests;

public class DeduplicationServiceTests
{
    // -----------------------------------------------------------------------
    // Unit tests — fake repositories, no DB
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CheckSourceItem_NeitherUrlNorHash_ReturnsNotDuplicate()
    {
        var service = BuildService(new FakeSourceItemRepo(), new FakeArticleRepo());

        var result = await service.CheckSourceItemAsync(null, null);

        Assert.False(result.IsDuplicate);
        Assert.Null(result.Reason);
    }

    [Fact]
    public async Task CheckSourceItem_CanonicalUrlExists_ReturnsDuplicate()
    {
        var repo = new FakeSourceItemRepo { ExistingCanonicalUrls = ["https://blog.whatsapp.com/article"] };
        var service = BuildService(repo, new FakeArticleRepo());

        var result = await service.CheckSourceItemAsync("https://blog.whatsapp.com/article", null);

        Assert.True(result.IsDuplicate);
        Assert.Contains("canonicalUrl", result.Reason);
    }

    [Fact]
    public async Task CheckSourceItem_ContentHashExists_ReturnsDuplicate()
    {
        var repo = new FakeSourceItemRepo { ExistingHashes = ["abc123"] };
        var service = BuildService(repo, new FakeArticleRepo());

        var result = await service.CheckSourceItemAsync(null, "abc123");

        Assert.True(result.IsDuplicate);
        Assert.Contains("contentHash", result.Reason);
    }

    [Fact]
    public async Task CheckSourceItem_UrlTakesPrecedenceOverHash()
    {
        // URL match is detected first even when hash also matches
        var repo = new FakeSourceItemRepo
        {
            ExistingCanonicalUrls = ["https://example.com/a"],
            ExistingHashes = ["xyz"]
        };
        var service = BuildService(repo, new FakeArticleRepo());

        var result = await service.CheckSourceItemAsync("https://example.com/a", "xyz");

        Assert.True(result.IsDuplicate);
        Assert.Contains("canonicalUrl", result.Reason);
    }

    [Fact]
    public async Task CheckSourceItem_NoMatchOnEither_ReturnsNotDuplicate()
    {
        var repo = new FakeSourceItemRepo
        {
            ExistingCanonicalUrls = ["https://other.com/article"],
            ExistingHashes = ["differenthash"]
        };
        var service = BuildService(repo, new FakeArticleRepo());

        var result = await service.CheckSourceItemAsync("https://blog.whatsapp.com/new", "newhash");

        Assert.False(result.IsDuplicate);
    }

    [Fact]
    public async Task ArticleExistsForSourceItem_WhenArticleExists_ReturnsTrue()
    {
        var sourceItemId = Guid.NewGuid();
        var articleRepo = new FakeArticleRepo { ExistingSourceItemIds = [sourceItemId] };
        var service = BuildService(new FakeSourceItemRepo(), articleRepo);

        var exists = await service.ArticleExistsForSourceItemAsync(sourceItemId);

        Assert.True(exists);
    }

    [Fact]
    public async Task ArticleExistsForSourceItem_WhenNoArticle_ReturnsFalse()
    {
        var service = BuildService(new FakeSourceItemRepo(), new FakeArticleRepo());

        var exists = await service.ArticleExistsForSourceItemAsync(Guid.NewGuid());

        Assert.False(exists);
    }

    // -----------------------------------------------------------------------
    // Integration tests — InMemory EF, real repositories
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Integration_CanonicalUrl_Deduplication_Works()
    {
        await using var db = CreateDbContext();

        var source = AddSource(db);
        var existingItem = new SourceItem
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            OriginalUrl = "https://blog.whatsapp.com/article-one",
            CanonicalUrl = "https://blog.whatsapp.com/article-one",
            Title = "Article One",
            ContentHash = "hash1"
        };
        db.SourceItems.Add(existingItem);
        await db.SaveChangesAsync();

        var sourceItemRepo = new EfSourceItemRepository(db);
        var articleRepo = new EfArticleRepository(db);
        var service = BuildService(sourceItemRepo, articleRepo);

        // Same canonical URL → duplicate
        var result = await service.CheckSourceItemAsync("https://blog.whatsapp.com/article-one", "differenthash");

        Assert.True(result.IsDuplicate);
    }

    [Fact]
    public async Task Integration_ContentHash_Deduplication_Works()
    {
        await using var db = CreateDbContext();

        var source = AddSource(db);
        var existingItem = new SourceItem
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            OriginalUrl = "https://blog.whatsapp.com/original",
            CanonicalUrl = "https://blog.whatsapp.com/original",
            Title = "Original Article",
            ContentHash = "knownhash"
        };
        db.SourceItems.Add(existingItem);
        await db.SaveChangesAsync();

        var sourceItemRepo = new EfSourceItemRepository(db);
        var articleRepo = new EfArticleRepository(db);
        var service = BuildService(sourceItemRepo, articleRepo);

        // Different URL but same content hash → duplicate
        var result = await service.CheckSourceItemAsync("https://blog.whatsapp.com/reposted", "knownhash");

        Assert.True(result.IsDuplicate);
    }

    [Fact]
    public async Task Integration_ArticleDuplicate_Works()
    {
        await using var db = CreateDbContext();

        var source = AddSource(db);
        var sourceItem = new SourceItem
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            OriginalUrl = "https://blog.whatsapp.com/item",
            Title = "Some Item"
        };
        db.SourceItems.Add(sourceItem);

        var article = new Article
        {
            Id = Guid.NewGuid(),
            SourceItemId = sourceItem.Id,
            Slug = "some-item",
            Title = "Some Item",
            Excerpt = "Excerpt",
            ContentHtml = "<p>Content</p>",
            MetaTitle = "Some Item",
            MetaDescription = "Desc",
            ArticleType = EditorialType.OfficialNews,
            Status = PipelineStatus.Draft
        };
        db.Articles.Add(article);
        await db.SaveChangesAsync();

        var sourceItemRepo = new EfSourceItemRepository(db);
        var articleRepo = new EfArticleRepository(db);
        var service = BuildService(sourceItemRepo, articleRepo);

        Assert.True(await service.ArticleExistsForSourceItemAsync(sourceItem.Id));
    }

    [Fact]
    public async Task Integration_Reexecution_DoesNotCreateDuplicateSourceItem()
    {
        await using var db = CreateDbContext();

        var source = AddSource(db);
        var canonicalUrl = "https://blog.whatsapp.com/repeated-article";

        // First time: item doesn't exist → not duplicate
        var sourceItemRepo = new EfSourceItemRepository(db);
        var service = BuildService(sourceItemRepo, new EfArticleRepository(db));

        var firstCheck = await service.CheckSourceItemAsync(canonicalUrl, "hash-a");
        Assert.False(firstCheck.IsDuplicate);

        // Persist the item
        var item = new SourceItem
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            OriginalUrl = canonicalUrl,
            CanonicalUrl = canonicalUrl,
            Title = "Repeated Article",
            ContentHash = "hash-a"
        };
        db.SourceItems.Add(item);
        await db.SaveChangesAsync();

        // Second time: same URL → duplicate
        var secondCheck = await service.CheckSourceItemAsync(canonicalUrl, "hash-a");
        Assert.True(secondCheck.IsDuplicate);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static DeduplicationService BuildService(
        ISourceItemRepository sourceItemRepo,
        IArticleRepository articleRepo)
    {
        return new DeduplicationService(
            sourceItemRepo,
            articleRepo,
            NullLogger<DeduplicationService>.Instance);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static Source AddSource(AppDbContext db)
    {
        var source = new Source
        {
            Id = Guid.NewGuid(),
            Name = "WhatsApp Blog",
            BaseUrl = "https://blog.whatsapp.com",
            Type = SourceType.Official,
            IsActive = true
        };
        db.Sources.Add(source);
        return source;
    }

    // -----------------------------------------------------------------------
    // Fake repositories
    // -----------------------------------------------------------------------

    private class FakeSourceItemRepo : ISourceItemRepository
    {
        public HashSet<string> ExistingCanonicalUrls { get; init; } = [];
        public HashSet<string> ExistingHashes { get; init; } = [];

        public Task<SourceItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<SourceItem?>(null);

        public Task<bool> ExistsByUrlAsync(string originalUrl, CancellationToken ct = default)
            => Task.FromResult(false);

        public Task<bool> ExistsByCanonicalUrlAsync(string canonicalUrl, CancellationToken ct = default)
            => Task.FromResult(ExistingCanonicalUrls.Contains(canonicalUrl));

        public Task<bool> ExistsByContentHashAsync(string contentHash, CancellationToken ct = default)
            => Task.FromResult(ExistingHashes.Contains(contentHash));

        public Task AddAsync(SourceItem item, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task UpdateAsync(SourceItem item, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private class FakeArticleRepo : IArticleRepository
    {
        public HashSet<Guid> ExistingSourceItemIds { get; init; } = [];

        public Task<Article?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<Article?>(null);

        public Task<Article?> GetBySlugAsync(string slug, CancellationToken ct = default)
            => Task.FromResult<Article?>(null);

        public Task<List<Article>> GetPublishedAsync(int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(new List<Article>());

        public Task<bool> ExistsBySourceItemIdAsync(Guid sourceItemId, CancellationToken ct = default)
            => Task.FromResult(ExistingSourceItemIds.Contains(sourceItemId));

        public Task AddAsync(Article article, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task UpdateAsync(Article article, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
