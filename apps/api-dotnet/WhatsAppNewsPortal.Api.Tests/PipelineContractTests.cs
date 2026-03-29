using System.Text.Json;
using WhatsAppNewsPortal.Api.AiGeneration.Application;
using WhatsAppNewsPortal.Api.Articles.Application;
using WhatsAppNewsPortal.Api.Common;
using WhatsAppNewsPortal.Api.ContentProcessing.Application;
using WhatsAppNewsPortal.Api.Demo.Application;
using WhatsAppNewsPortal.Api.Ingestion.Application;

namespace WhatsAppNewsPortal.Api.Tests;

public class PipelineContractTests
{
    // ── DiscoveredItemDto ────────────────────────────────────────────────────

    [Fact]
    public void DiscoveredItemDto_Defaults_AreValid()
    {
        var dto = new DiscoveredItemDto
        {
            SourceId = Guid.NewGuid(),
            OriginalUrl = "https://blog.whatsapp.com/post",
            Title = "New feature"
        };

        Assert.False(dto.IsDemoItem);
        Assert.Null(dto.RawContent);
        Assert.Null(dto.PublishedAt);
    }

    [Fact]
    public void DiscoveredItemDto_DemoFlag_SetCorrectly()
    {
        var dto = new DiscoveredItemDto { IsDemoItem = true };
        Assert.True(dto.IsDemoItem);
    }

    // ── NormalizedItemDto ────────────────────────────────────────────────────

    [Fact]
    public void NormalizedItemDto_JsonRoundTrip_PreservesAllFields()
    {
        var original = new NormalizedItemDto
        {
            SourceItemId = Guid.NewGuid(),
            SourceId = Guid.NewGuid(),
            OriginalUrl = "https://wabetainfo.com/feature-beta",
            CanonicalUrl = "https://wabetainfo.com/feature-beta",
            Title = "Beta feature discovered",
            NormalizedContent = "WhatsApp is testing a new feature...",
            ContentHash = "sha256:abc123",
            PublishedAt = new DateTime(2026, 3, 28, 10, 0, 0, DateTimeKind.Utc),
            SourceType = SourceType.BetaSpecialized,
            IsDemoItem = true
        };

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<NormalizedItemDto>(json);

        Assert.NotNull(restored);
        Assert.Equal(original.SourceItemId, restored.SourceItemId);
        Assert.Equal(original.CanonicalUrl, restored.CanonicalUrl);
        Assert.Equal(original.ContentHash, restored.ContentHash);
        Assert.Equal(SourceType.BetaSpecialized, restored.SourceType);
        Assert.True(restored.IsDemoItem);
    }

    // ── ClassificationResultDto ──────────────────────────────────────────────

    [Fact]
    public void ClassificationResultDto_BetaSource_MustHaveBetaNewsType()
    {
        // The rule: items from beta_specialized must always yield BetaNews.
        // This test documents the contract; enforcement is in the IAiClassifier implementation.
        var result = new ClassificationResultDto
        {
            IsRelevant = true,
            EditorialType = EditorialType.BetaNews,
            SuggestedTitle = "WhatsApp testa novo recurso em beta",
            Slug = "whatsapp-testa-novo-recurso-beta",
            MetaTitle = "WhatsApp testa novo recurso | Portal WhatsApp",
            MetaDescription = "Descubra o que o WABetaInfo descobriu sobre o próximo recurso do WhatsApp.",
            Excerpt = "O WhatsApp está testando um novo recurso que pode chegar em breve.",
            Tags = ["beta", "whatsapp", "novidade"],
            EditorialNote = "Fonte: WABetaInfo — conteúdo em fase de testes, não confirmado oficialmente."
        };

        Assert.Equal(EditorialType.BetaNews, result.EditorialType);
        Assert.NotNull(result.EditorialNote);
        Assert.Contains("beta", result.Tags);
    }

    [Fact]
    public void ClassificationResultDto_IrrelevantItem_HasDiscardReason()
    {
        var result = new ClassificationResultDto
        {
            IsRelevant = false,
            DiscardReason = "Conteúdo não relacionado ao ecossistema WhatsApp"
        };

        Assert.False(result.IsRelevant);
        Assert.NotEmpty(result.DiscardReason!);
    }

    [Fact]
    public void ClassificationResultDto_JsonRoundTrip_PreservesAllFields()
    {
        var original = new ClassificationResultDto
        {
            IsRelevant = true,
            EditorialType = EditorialType.OfficialNews,
            SuggestedTitle = "WhatsApp lança nova funcionalidade",
            Slug = "whatsapp-lanca-nova-funcionalidade",
            MetaTitle = "WhatsApp lança nova funcionalidade | Portal WhatsApp",
            MetaDescription = "O WhatsApp anunciou oficialmente uma nova funcionalidade para todos os usuários.",
            Excerpt = "A empresa anunciou o lançamento.",
            Tags = ["whatsapp", "lançamento", "oficial"],
            EditorialNote = null
        };

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<ClassificationResultDto>(json);

        Assert.NotNull(restored);
        Assert.True(restored.IsRelevant);
        Assert.Equal(EditorialType.OfficialNews, restored.EditorialType);
        Assert.Equal(original.Slug, restored.Slug);
        Assert.Equal(3, restored.Tags.Length);
        Assert.Null(restored.EditorialNote);
    }

    // ── GeneratedArticleDto ──────────────────────────────────────────────────

    [Fact]
    public void GeneratedArticleDto_BetaItem_HasBetaDisclaimer()
    {
        var dto = new GeneratedArticleDto
        {
            Title = "WhatsApp testa recurso em fase beta",
            Excerpt = "Novo recurso detectado em versão de testes.",
            ContentHtml = "<h2>O que foi descoberto</h2><p>...</p>",
            BetaDisclaimer = "Este conteúdo é baseado em informações do WABetaInfo e refere-se a funcionalidades ainda em fase de testes, sem confirmação oficial do WhatsApp."
        };

        Assert.NotNull(dto.BetaDisclaimer);
        Assert.NotEmpty(dto.BetaDisclaimer);
    }

    [Fact]
    public void GeneratedArticleDto_OfficialItem_HasNoBetaDisclaimer()
    {
        var dto = new GeneratedArticleDto
        {
            Title = "WhatsApp anuncia atualização oficial",
            Excerpt = "Recurso disponível para todos.",
            ContentHtml = "<h2>Detalhes</h2><p>...</p>",
            BetaDisclaimer = null
        };

        Assert.Null(dto.BetaDisclaimer);
    }

    [Fact]
    public void GeneratedArticleDto_JsonRoundTrip_PreservesAllFields()
    {
        var original = new GeneratedArticleDto
        {
            Title = "Título do artigo",
            Excerpt = "Resumo do artigo.",
            ContentHtml = "<p>Conteúdo</p>",
            BetaDisclaimer = "Em fase beta."
        };

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<GeneratedArticleDto>(json);

        Assert.NotNull(restored);
        Assert.Equal(original.Title, restored.Title);
        Assert.Equal(original.BetaDisclaimer, restored.BetaDisclaimer);
    }

    // ── PublishArticleResultDto ──────────────────────────────────────────────

    [Fact]
    public void PublishArticleResultDto_JsonRoundTrip_PreservesAllFields()
    {
        var publishedAt = new DateTime(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc);
        var original = new PublishArticleResultDto
        {
            ArticleId = Guid.NewGuid(),
            Slug = "whatsapp-lanca-funcionalidade",
            PublishedAt = publishedAt
        };

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<PublishArticleResultDto>(json);

        Assert.NotNull(restored);
        Assert.Equal(original.ArticleId, restored.ArticleId);
        Assert.Equal(original.Slug, restored.Slug);
        Assert.Equal(publishedAt, restored.PublishedAt);
    }

    // ── DemoPipelineResultDto ────────────────────────────────────────────────

    [Fact]
    public void DemoPipelineResultDto_Defaults_AreValid()
    {
        var result = new DemoPipelineResultDto();

        Assert.False(result.Success);
        Assert.Equal(string.Empty, result.Url);
        Assert.Null(result.SourceItemId);
        Assert.Null(result.ArticleId);
        Assert.Empty(result.Steps);
    }

    [Fact]
    public void DemoPipelineResultDto_ErrorMessage_TracksFailure()
    {
        var result = new DemoPipelineResultDto
        {
            Success = false,
            Url = "https://blog.whatsapp.com/demo",
            ErrorMessage = "Failed to fetch content"
        };

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void DemoPipelineResultDto_JsonRoundTrip_PreservesAllFields()
    {
        var executedAt = new DateTime(2026, 3, 28, 14, 0, 0, DateTimeKind.Utc);
        var original = new DemoPipelineResultDto
        {
            Success = true,
            Url = "https://blog.whatsapp.com/demo-post",
            SourceName = "WhatsApp Blog",
            WasReset = true,
            SourceItemId = Guid.NewGuid(),
            ArticleId = Guid.NewGuid(),
            Slug = "demo-post-whatsapp",
            Status = "draft",
            Steps = ["Source matched", "Content fetched", "Article generated"],
            ExecutedAt = executedAt
        };

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<DemoPipelineResultDto>(json);

        Assert.NotNull(restored);
        Assert.True(restored.Success);
        Assert.Equal("https://blog.whatsapp.com/demo-post", restored.Url);
        Assert.Equal("WhatsApp Blog", restored.SourceName);
        Assert.True(restored.WasReset);
        Assert.Equal(original.SourceItemId, restored.SourceItemId);
        Assert.Equal(original.ArticleId, restored.ArticleId);
        Assert.Equal("demo-post-whatsapp", restored.Slug);
        Assert.Equal(3, restored.Steps.Count);
        Assert.Equal(executedAt, restored.ExecutedAt);
    }
}
