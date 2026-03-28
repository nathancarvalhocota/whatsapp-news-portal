using Microsoft.EntityFrameworkCore;
using WhatsAppNewsPortal.Api.Articles.Domain;
using WhatsAppNewsPortal.Api.Common;
using WhatsAppNewsPortal.Api.Infrastructure.Data;
using WhatsAppNewsPortal.Api.Sources.Domain;

namespace WhatsAppNewsPortal.Api.Tests;

public class DbContextTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task CanPersistAndReadSource()
    {
        await using var context = CreateContext();

        var source = new Source
        {
            Id = Guid.NewGuid(),
            Name = "WhatsApp Blog",
            BaseUrl = "https://blog.whatsapp.com",
            FeedUrl = "https://blog.whatsapp.com/feed",
            Type = SourceType.Official,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Sources.Add(source);
        await context.SaveChangesAsync();

        var loaded = await context.Sources.FirstOrDefaultAsync(s => s.Id == source.Id);

        Assert.NotNull(loaded);
        Assert.Equal("WhatsApp Blog", loaded.Name);
        Assert.Equal(SourceType.Official, loaded.Type);
        Assert.Equal("https://blog.whatsapp.com/feed", loaded.FeedUrl);
        Assert.True(loaded.IsActive);
    }

    [Fact]
    public async Task CanPersistSourceItemLinkedToSource()
    {
        await using var context = CreateContext();

        var source = new Source
        {
            Id = Guid.NewGuid(),
            Name = "WABetaInfo",
            BaseUrl = "https://wabetainfo.com",
            Type = SourceType.BetaSpecialized
        };

        var item = new SourceItem
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            OriginalUrl = "https://wabetainfo.com/some-post",
            CanonicalUrl = "https://wabetainfo.com/some-post",
            Title = "New feature in beta",
            ContentHash = "abc123",
            IsDemoItem = false,
            SourceClassification = "beta",
            Status = PipelineStatus.Discovered,
            Source = source
        };

        context.Sources.Add(source);
        context.SourceItems.Add(item);
        await context.SaveChangesAsync();

        var loaded = await context.SourceItems
            .Include(si => si.Source)
            .FirstOrDefaultAsync(si => si.Id == item.Id);

        Assert.NotNull(loaded);
        Assert.Equal(source.Id, loaded.SourceId);
        Assert.Equal("WABetaInfo", loaded.Source.Name);
        Assert.Equal(PipelineStatus.Discovered, loaded.Status);
        Assert.Equal("abc123", loaded.ContentHash);
        Assert.Equal("beta", loaded.SourceClassification);
        Assert.False(loaded.IsDemoItem);
    }

    [Fact]
    public async Task CanPersistArticleLinkedToSourceItem()
    {
        await using var context = CreateContext();

        var source = new Source
        {
            Id = Guid.NewGuid(),
            Name = "WhatsApp Blog",
            BaseUrl = "https://blog.whatsapp.com",
            Type = SourceType.Official
        };

        var item = new SourceItem
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            OriginalUrl = "https://blog.whatsapp.com/new-feature",
            Title = "New Feature Announced",
            Status = PipelineStatus.Processing,
            Source = source
        };

        var article = new Article
        {
            Id = Guid.NewGuid(),
            SourceItemId = item.Id,
            Title = "WhatsApp anuncia nova funcionalidade",
            Slug = "whatsapp-anuncia-nova-funcionalidade",
            Excerpt = "Resumo do artigo",
            ContentHtml = "<p>Conteudo do artigo</p>",
            MetaTitle = "WhatsApp anuncia nova funcionalidade | Portal WhatsApp",
            MetaDescription = "Meta description do artigo",
            Category = "official",
            ArticleType = EditorialType.OfficialNews,
            Status = PipelineStatus.Draft,
            Tags = ["whatsapp", "novidade"],
            SourceItem = item
        };

        context.Sources.Add(source);
        context.SourceItems.Add(item);
        context.Articles.Add(article);
        await context.SaveChangesAsync();

        var loaded = await context.Articles
            .Include(a => a.SourceItem)
            .FirstOrDefaultAsync(a => a.Id == article.Id);

        Assert.NotNull(loaded);
        Assert.Equal("whatsapp-anuncia-nova-funcionalidade", loaded.Slug);
        Assert.Equal(EditorialType.OfficialNews, loaded.ArticleType);
        Assert.Equal(PipelineStatus.Draft, loaded.Status);
        Assert.Equal(2, loaded.Tags.Length);
        Assert.Equal("official", loaded.Category);
        Assert.Equal("WhatsApp anuncia nova funcionalidade | Portal WhatsApp", loaded.MetaTitle);
        Assert.Equal(item.Id, loaded.SourceItemId);
    }

    [Fact]
    public async Task CanPersistArticleSourceReference()
    {
        await using var context = CreateContext();

        var source = new Source
        {
            Id = Guid.NewGuid(),
            Name = "WhatsApp Blog",
            BaseUrl = "https://blog.whatsapp.com",
            Type = SourceType.Official
        };

        var item = new SourceItem
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            OriginalUrl = "https://blog.whatsapp.com/post",
            Title = "Post",
            Status = PipelineStatus.Discovered,
            Source = source
        };

        var article = new Article
        {
            Id = Guid.NewGuid(),
            SourceItemId = item.Id,
            Title = "Artigo",
            Slug = "artigo-teste-ref",
            Excerpt = "Resumo",
            ContentHtml = "<p>Conteudo</p>",
            MetaTitle = "Artigo Teste",
            MetaDescription = "Descricao",
            ArticleType = EditorialType.OfficialNews,
            Status = PipelineStatus.Draft,
            Tags = [],
            SourceItem = item
        };

        var reference = new ArticleSourceReference
        {
            Id = Guid.NewGuid(),
            ArticleId = article.Id,
            SourceName = "WhatsApp Blog",
            SourceUrl = "https://blog.whatsapp.com/post",
            ReferenceType = "primary",
            Article = article
        };

        context.Sources.Add(source);
        context.SourceItems.Add(item);
        context.Articles.Add(article);
        context.ArticleSourceReferences.Add(reference);
        await context.SaveChangesAsync();

        var loaded = await context.ArticleSourceReferences
            .Include(r => r.Article)
            .FirstOrDefaultAsync(r => r.Id == reference.Id);

        Assert.NotNull(loaded);
        Assert.Equal(article.Id, loaded.ArticleId);
        Assert.Equal("primary", loaded.ReferenceType);
        Assert.Equal("WhatsApp Blog", loaded.SourceName);
    }

    [Fact]
    public async Task CanPersistProcessingLog()
    {
        await using var context = CreateContext();

        var source = new Source
        {
            Id = Guid.NewGuid(),
            Name = "WhatsApp Blog",
            BaseUrl = "https://blog.whatsapp.com",
            Type = SourceType.Official
        };

        var item = new SourceItem
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            OriginalUrl = "https://blog.whatsapp.com/post2",
            Title = "Post 2",
            Status = PipelineStatus.Discovered,
            Source = source
        };

        context.Sources.Add(source);
        context.SourceItems.Add(item);
        await context.SaveChangesAsync();

        var log = new ProcessingLog
        {
            Id = Guid.NewGuid(),
            SourceItemId = item.Id,
            StepName = "classification",
            Status = "success",
            Message = "Item classificado com sucesso",
            CreatedAt = DateTime.UtcNow
        };

        context.ProcessingLogs.Add(log);
        await context.SaveChangesAsync();

        var loaded = await context.ProcessingLogs
            .FirstOrDefaultAsync(l => l.Id == log.Id);

        Assert.NotNull(loaded);
        Assert.Equal(item.Id, loaded.SourceItemId);
        Assert.Equal("classification", loaded.StepName);
        Assert.Equal("success", loaded.Status);
    }

    [Fact]
    public async Task CanPersistProcessingLogWithoutSourceItem()
    {
        await using var context = CreateContext();

        var log = new ProcessingLog
        {
            Id = Guid.NewGuid(),
            SourceItemId = null,
            StepName = "ingestion",
            Status = "failed",
            Message = "Feed indisponivel",
            CreatedAt = DateTime.UtcNow
        };

        context.ProcessingLogs.Add(log);
        await context.SaveChangesAsync();

        var loaded = await context.ProcessingLogs
            .FirstOrDefaultAsync(l => l.Id == log.Id);

        Assert.NotNull(loaded);
        Assert.Null(loaded.SourceItemId);
        Assert.Equal("failed", loaded.Status);
    }
}
