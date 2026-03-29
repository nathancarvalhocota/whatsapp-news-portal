using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WhatsAppNewsPortal.Api.Articles.Application;
using WhatsAppNewsPortal.Api.Articles.Domain;
using WhatsAppNewsPortal.Api.Common;
using WhatsAppNewsPortal.Api.Sources.Application;
using WhatsAppNewsPortal.Api.Sources.Domain;

namespace WhatsAppNewsPortal.Api.Tests;

public class ArticleEndpointTests
{
    private static WebApplicationFactory<Program> CreateFactory(
        IArticleRepository? articleRepo = null,
        ISourceRepository? sourceRepo = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                if (articleRepo != null)
                {
                    var d = services.SingleOrDefault(s => s.ServiceType == typeof(IArticleRepository));
                    if (d != null) services.Remove(d);
                    services.AddScoped(_ => articleRepo);
                }
                if (sourceRepo != null)
                {
                    var d = services.SingleOrDefault(s => s.ServiceType == typeof(ISourceRepository));
                    if (d != null) services.Remove(d);
                    services.AddScoped(_ => sourceRepo);
                }
            });
        });

    // ── GET /api/articles/published ─────────────────────────────────────────

    [Fact]
    public async Task GetPublishedArticles_EmptyList_ReturnsEmptyArray()
    {
        var client = CreateFactory(articleRepo: new StubArticleRepo()).CreateClient();

        var response = await client.GetAsync("/api/articles/published");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(0, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task GetPublishedArticles_WithArticle_ReturnsSummaryFields()
    {
        var article = PublishedArticle("whatsapp-lancamento", "oficial");
        var client = CreateFactory(articleRepo: new StubArticleRepo(published: [article])).CreateClient();

        var response = await client.GetAsync("/api/articles/published");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(1, doc.RootElement.GetArrayLength());

        var item = doc.RootElement[0];
        Assert.Equal("whatsapp-lancamento", item.GetProperty("slug").GetString());
        Assert.True(item.TryGetProperty("title", out _));
        Assert.True(item.TryGetProperty("excerpt", out _));
        Assert.True(item.TryGetProperty("metaDescription", out _));
        Assert.True(item.TryGetProperty("tags", out _));
        Assert.True(item.TryGetProperty("articleType", out _));
        Assert.True(item.TryGetProperty("publishedAt", out _));
        // Internal fields must not be exposed
        Assert.False(item.TryGetProperty("contentHtml", out _));
        Assert.False(item.TryGetProperty("sourceItemId", out _));
        Assert.False(item.TryGetProperty("status", out _));
    }

    // ── GET /api/articles/{slug} ─────────────────────────────────────────────

    [Fact]
    public async Task GetArticleBySlug_NotFound_Returns404()
    {
        var client = CreateFactory(articleRepo: new StubArticleRepo()).CreateClient();

        var response = await client.GetAsync("/api/articles/slug-inexistente");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetArticleBySlug_DraftArticle_Returns404()
    {
        var draft = DraftArticle("meu-rascunho");
        var client = CreateFactory(articleRepo: new StubArticleRepo(bySlug: draft)).CreateClient();

        var response = await client.GetAsync("/api/articles/meu-rascunho");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetArticleBySlug_PublishedArticle_ReturnsDetailFields()
    {
        var article = PublishedArticle("whatsapp-novo-recurso", "beta");
        var client = CreateFactory(articleRepo: new StubArticleRepo(bySlug: article)).CreateClient();

        var response = await client.GetAsync("/api/articles/whatsapp-novo-recurso");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("whatsapp-novo-recurso", root.GetProperty("slug").GetString());
        Assert.True(root.TryGetProperty("contentHtml", out _));
        Assert.True(root.TryGetProperty("metaTitle", out _));
        Assert.True(root.TryGetProperty("metaDescription", out _));
        Assert.True(root.TryGetProperty("sourceReferences", out _));
        // Internal fields must not be exposed
        Assert.False(root.TryGetProperty("sourceItemId", out _));
        Assert.False(root.TryGetProperty("status", out _));
    }

    // ── GET /api/articles/published (literal route precedes slug route) ───────

    [Fact]
    public async Task GetPublishedArticles_RouteTakesPrecedenceOverSlug()
    {
        // If slug = "published", the list endpoint should respond, not the slug endpoint
        var client = CreateFactory(articleRepo: new StubArticleRepo()).CreateClient();

        var response = await client.GetAsync("/api/articles/published");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    // ── GET /api/categories/{category} ──────────────────────────────────────

    [Fact]
    public async Task GetCategoryArticles_EmptyList_ReturnsEmptyArray()
    {
        var client = CreateFactory(articleRepo: new StubArticleRepo()).CreateClient();

        var response = await client.GetAsync("/api/categories/oficial");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(0, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task GetCategoryArticles_WithArticle_ReturnsSummaryShape()
    {
        var article = PublishedArticle("artigo-oficial", "oficial");
        var client = CreateFactory(articleRepo: new StubArticleRepo(byCategory: [article])).CreateClient();

        var response = await client.GetAsync("/api/categories/oficial");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(1, doc.RootElement.GetArrayLength());
        Assert.Equal("artigo-oficial", doc.RootElement[0].GetProperty("slug").GetString());
    }

    // ── GET /api/sources ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetSources_EmptyList_ReturnsEmptyArray()
    {
        var client = CreateFactory(sourceRepo: new StubSourceRepo()).CreateClient();

        var response = await client.GetAsync("/api/sources");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(0, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task GetSources_WithSource_ReturnsPublicFields()
    {
        var source = new Source
        {
            Id = Guid.NewGuid(),
            Name = "WhatsApp Blog",
            Type = SourceType.Official,
            BaseUrl = "https://blog.whatsapp.com",
            FeedUrl = "https://blog.whatsapp.com/rss.xml",
            IsActive = true
        };
        var client = CreateFactory(sourceRepo: new StubSourceRepo([source])).CreateClient();

        var response = await client.GetAsync("/api/sources");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(1, doc.RootElement.GetArrayLength());

        var src = doc.RootElement[0];
        Assert.Equal("WhatsApp Blog", src.GetProperty("name").GetString());
        Assert.True(src.TryGetProperty("type", out _));
        Assert.True(src.TryGetProperty("baseUrl", out _));
        Assert.True(src.TryGetProperty("isActive", out _));
        // feedUrl is an internal field, should not be exposed
        Assert.False(src.TryGetProperty("feedUrl", out _));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Article PublishedArticle(string slug, string category) => new()
    {
        Id = Guid.NewGuid(),
        SourceItemId = Guid.NewGuid(),
        Slug = slug,
        Title = "Título do artigo de teste",
        Excerpt = "Resumo do artigo de teste.",
        ContentHtml = "<p>Conteúdo do artigo.</p>",
        MetaTitle = "Título SEO",
        MetaDescription = "Descrição SEO.",
        Tags = ["whatsapp", "teste"],
        ArticleType = EditorialType.OfficialNews,
        Category = category,
        Status = PipelineStatus.Published,
        PublishedAt = DateTime.UtcNow
    };

    private static Article DraftArticle(string slug) => new()
    {
        Id = Guid.NewGuid(),
        SourceItemId = Guid.NewGuid(),
        Slug = slug,
        Title = "Rascunho",
        Excerpt = "Resumo.",
        ContentHtml = "<p>Conteúdo.</p>",
        MetaTitle = "Meta título",
        MetaDescription = "Meta descrição.",
        Tags = [],
        ArticleType = EditorialType.OfficialNews,
        Status = PipelineStatus.Draft,
        PublishedAt = null
    };

    // ── Stubs ─────────────────────────────────────────────────────────────────

    private class StubArticleRepo : IArticleRepository
    {
        private readonly List<Article> _published;
        private readonly List<Article> _byCategory;
        private readonly Article? _bySlug;

        public StubArticleRepo(
            List<Article>? published = null,
            List<Article>? byCategory = null,
            Article? bySlug = null)
        {
            _published = published ?? [];
            _byCategory = byCategory ?? [];
            _bySlug = bySlug;
        }

        public Task<Article?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<Article?>(null);

        public Task<Article?> GetBySlugAsync(string slug, CancellationToken ct = default)
            => Task.FromResult(_bySlug?.Slug == slug ? _bySlug : null);

        public Task<List<Article>> GetPublishedAsync(int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(_published);

        public Task<List<Article>> GetByCategoryAsync(string category, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(_byCategory);

        public Task<bool> ExistsBySourceItemIdAsync(Guid sourceItemId, CancellationToken ct = default)
            => Task.FromResult(false);

        public Task AddAsync(Article article, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task UpdateAsync(Article article, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private class StubSourceRepo : ISourceRepository
    {
        private readonly List<Source> _sources;

        public StubSourceRepo(List<Source>? sources = null)
            => _sources = sources ?? [];

        public Task<List<Source>> GetActiveSourcesAsync(CancellationToken ct = default)
            => Task.FromResult(_sources);

        public Task<Source?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<Source?>(null);
    }
}
