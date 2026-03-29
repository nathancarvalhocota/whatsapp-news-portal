using WhatsAppNewsPortal.Api.Articles.Application;
using WhatsAppNewsPortal.Api.Articles.Domain;
using WhatsAppNewsPortal.Api.Articles.Infrastructure;
using WhatsAppNewsPortal.Api.Common;

namespace WhatsAppNewsPortal.Api.Tests;

public class ArticlePublisherTests
{
    // =====================================================================
    // Publication — happy path
    // =====================================================================

    [Fact]
    public async Task Publish_ValidDraft_TransitionsToPublished()
    {
        var article = ValidDraftArticle();
        var repo = new InMemoryArticleRepo(article);
        var publisher = new ArticlePublisher(repo, Microsoft.Extensions.Logging.Abstractions.NullLogger<ArticlePublisher>.Instance);

        var result = await publisher.PublishAsync(article.Id);

        Assert.Equal(article.Id, result.ArticleId);
        Assert.Equal(article.Slug, result.Slug);
        Assert.Equal(PipelineStatus.Published, repo.LastUpdated!.Status);
        Assert.NotNull(repo.LastUpdated.PublishedAt);
        Assert.Equal(result.PublishedAt, repo.LastUpdated.PublishedAt!.Value);
    }

    // =====================================================================
    // Idempotency — double publish
    // =====================================================================

    [Fact]
    public async Task Publish_AlreadyPublished_ReturnsExistingData()
    {
        var publishedAt = new DateTime(2026, 3, 20, 10, 0, 0, DateTimeKind.Utc);
        var article = ValidDraftArticle();
        article.Status = PipelineStatus.Published;
        article.PublishedAt = publishedAt;

        var repo = new InMemoryArticleRepo(article);
        var publisher = new ArticlePublisher(repo, Microsoft.Extensions.Logging.Abstractions.NullLogger<ArticlePublisher>.Instance);

        var result = await publisher.PublishAsync(article.Id);

        Assert.Equal(article.Id, result.ArticleId);
        Assert.Equal(article.Slug, result.Slug);
        Assert.Equal(publishedAt, result.PublishedAt);
        // Should NOT have called UpdateAsync
        Assert.Null(repo.LastUpdated);
    }

    [Fact]
    public async Task Publish_Twice_SecondCallReturnsSameResult()
    {
        var article = ValidDraftArticle();
        var repo = new InMemoryArticleRepo(article);
        var publisher = new ArticlePublisher(repo, Microsoft.Extensions.Logging.Abstractions.NullLogger<ArticlePublisher>.Instance);

        var first = await publisher.PublishAsync(article.Id);
        var second = await publisher.PublishAsync(article.Id);

        Assert.Equal(first.ArticleId, second.ArticleId);
        Assert.Equal(first.Slug, second.Slug);
        Assert.Equal(first.PublishedAt, second.PublishedAt);
    }

    // =====================================================================
    // Validation — invalid drafts
    // =====================================================================

    [Fact]
    public async Task Publish_ArticleNotFound_Throws()
    {
        var repo = new InMemoryArticleRepo(null);
        var publisher = new ArticlePublisher(repo, Microsoft.Extensions.Logging.Abstractions.NullLogger<ArticlePublisher>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => publisher.PublishAsync(Guid.NewGuid()));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task Publish_NotDraftStatus_Throws()
    {
        var article = ValidDraftArticle();
        article.Status = PipelineStatus.Failed;

        var repo = new InMemoryArticleRepo(article);
        var publisher = new ArticlePublisher(repo, Microsoft.Extensions.Logging.Abstractions.NullLogger<ArticlePublisher>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => publisher.PublishAsync(article.Id));

        Assert.Contains("not a draft", ex.Message);
    }

    [Fact]
    public async Task Publish_ProcessingStatus_Throws()
    {
        var article = ValidDraftArticle();
        article.Status = PipelineStatus.Processing;

        var repo = new InMemoryArticleRepo(article);
        var publisher = new ArticlePublisher(repo, Microsoft.Extensions.Logging.Abstractions.NullLogger<ArticlePublisher>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => publisher.PublishAsync(article.Id));

        Assert.Contains("not a draft", ex.Message);
    }

    [Fact]
    public async Task Publish_DraftMissingTitle_Throws()
    {
        var article = ValidDraftArticle();
        article.Title = "";

        var repo = new InMemoryArticleRepo(article);
        var publisher = new ArticlePublisher(repo, Microsoft.Extensions.Logging.Abstractions.NullLogger<ArticlePublisher>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => publisher.PublishAsync(article.Id));

        Assert.Contains("Title", ex.Message);
    }

    [Fact]
    public async Task Publish_DraftMissingContentHtml_Throws()
    {
        var article = ValidDraftArticle();
        article.ContentHtml = "";

        var repo = new InMemoryArticleRepo(article);
        var publisher = new ArticlePublisher(repo, Microsoft.Extensions.Logging.Abstractions.NullLogger<ArticlePublisher>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => publisher.PublishAsync(article.Id));

        Assert.Contains("ContentHtml", ex.Message);
    }

    [Fact]
    public async Task Publish_DraftWithPlainTextContent_Throws()
    {
        var article = ValidDraftArticle();
        article.ContentHtml = "Texto sem tags HTML";

        var repo = new InMemoryArticleRepo(article);
        var publisher = new ArticlePublisher(repo, Microsoft.Extensions.Logging.Abstractions.NullLogger<ArticlePublisher>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => publisher.PublishAsync(article.Id));

        Assert.Contains("HTML", ex.Message);
    }

    [Fact]
    public async Task Publish_DraftMissingSlug_Throws()
    {
        var article = ValidDraftArticle();
        article.Slug = "";

        var repo = new InMemoryArticleRepo(article);
        var publisher = new ArticlePublisher(repo, Microsoft.Extensions.Logging.Abstractions.NullLogger<ArticlePublisher>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => publisher.PublishAsync(article.Id));

        Assert.Contains("Slug", ex.Message);
    }

    [Fact]
    public async Task Publish_DraftMissingMultipleFields_ReportsAll()
    {
        var article = ValidDraftArticle();
        article.Title = "";
        article.Excerpt = "";
        article.Slug = "";

        var repo = new InMemoryArticleRepo(article);
        var publisher = new ArticlePublisher(repo, Microsoft.Extensions.Logging.Abstractions.NullLogger<ArticlePublisher>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => publisher.PublishAsync(article.Id));

        Assert.Contains("Title", ex.Message);
        Assert.Contains("Excerpt", ex.Message);
        Assert.Contains("Slug", ex.Message);
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    private static Article ValidDraftArticle() => new()
    {
        Id = Guid.NewGuid(),
        SourceItemId = Guid.NewGuid(),
        Slug = "whatsapp-lanca-compartilhamento-de-tela",
        Title = "WhatsApp lanca compartilhamento de tela",
        Excerpt = "O WhatsApp anunciou o recurso de compartilhamento de tela.",
        ContentHtml = "<p>Conteudo do artigo sobre compartilhamento de tela.</p>",
        MetaTitle = "WhatsApp lanca compartilhamento de tela",
        MetaDescription = "O WhatsApp lanca compartilhamento de tela em videochamadas.",
        Tags = ["whatsapp", "videochamada"],
        ArticleType = EditorialType.OfficialNews,
        Status = PipelineStatus.Draft,
        PublishedAt = null,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    // =====================================================================
    // Fakes
    // =====================================================================

    private class InMemoryArticleRepo : IArticleRepository
    {
        private Article? _article;
        public Article? LastUpdated { get; private set; }

        public InMemoryArticleRepo(Article? article) => _article = article;

        public Task<Article?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(_article?.Id == id ? _article : null);

        public Task<Article?> GetBySlugAsync(string slug, CancellationToken ct = default)
            => Task.FromResult<Article?>(null);

        public Task<List<Article>> GetPublishedAsync(int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(new List<Article>());

        public Task<List<Article>> GetByCategoryAsync(string category, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(new List<Article>());

        public Task<bool> ExistsBySourceItemIdAsync(Guid sourceItemId, CancellationToken ct = default)
            => Task.FromResult(false);

        public Task AddAsync(Article article, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task UpdateAsync(Article article, CancellationToken ct = default)
        {
            LastUpdated = article;
            return Task.CompletedTask;
        }
    }
}
