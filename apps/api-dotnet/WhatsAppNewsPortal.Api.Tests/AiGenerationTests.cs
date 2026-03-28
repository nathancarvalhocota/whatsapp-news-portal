using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WhatsAppNewsPortal.Api.AiGeneration.Application;
using WhatsAppNewsPortal.Api.AiGeneration.Infrastructure;
using WhatsAppNewsPortal.Api.Common;
using WhatsAppNewsPortal.Api.ContentProcessing.Application;

namespace WhatsAppNewsPortal.Api.Tests;

public class AiGenerationTests
{
    // -----------------------------------------------------------------------
    // Classification tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Classifier_ParsesValidOfficialResponse()
    {
        var provider = new FakeProvider("""
            {
                "isRelevant": true,
                "discardReason": null,
                "editorialType": "OfficialNews",
                "suggestedTitle": "WhatsApp lança nova funcionalidade",
                "slug": "whatsapp-lanca-nova-funcionalidade",
                "metaTitle": "WhatsApp lança nova funcionalidade",
                "metaDescription": "O WhatsApp anunciou uma nova funcionalidade para todos os usuários.",
                "excerpt": "Nova funcionalidade do WhatsApp anunciada oficialmente.",
                "tags": ["whatsapp", "novidade"],
                "editorialNote": null
            }
            """);

        var classifier = BuildClassifier(provider);
        var item = BuildNormalizedItem(SourceType.Official);

        var result = await classifier.ClassifyAsync(item);

        Assert.True(result.IsRelevant);
        Assert.Equal(EditorialType.OfficialNews, result.EditorialType);
        Assert.Equal("whatsapp-lanca-nova-funcionalidade", result.Slug);
        Assert.Contains("whatsapp", result.Tags);
        Assert.Null(result.DiscardReason);
    }

    [Fact]
    public async Task Classifier_EnforcesBetaNewsForBetaSource()
    {
        var provider = new FakeProvider("""
            {
                "isRelevant": true,
                "editorialType": "OfficialNews",
                "suggestedTitle": "WhatsApp testa recurso",
                "slug": "whatsapp-testa-recurso",
                "metaTitle": "WhatsApp testa recurso",
                "metaDescription": "Recurso em teste.",
                "excerpt": "Recurso em fase de testes.",
                "tags": ["whatsapp", "beta"],
                "editorialNote": null
            }
            """);

        var classifier = BuildClassifier(provider);
        var item = BuildNormalizedItem(SourceType.BetaSpecialized);

        var result = await classifier.ClassifyAsync(item);

        // Even though the AI returned OfficialNews, the classifier must enforce BetaNews
        Assert.Equal(EditorialType.BetaNews, result.EditorialType);
        Assert.NotNull(result.EditorialNote);
        Assert.NotEmpty(result.EditorialNote!);
    }

    [Fact]
    public async Task Classifier_PreservesEditorialNoteWhenProvided()
    {
        var provider = new FakeProvider("""
            {
                "isRelevant": true,
                "editorialType": "BetaNews",
                "suggestedTitle": "Recurso beta",
                "slug": "recurso-beta",
                "metaTitle": "Recurso beta",
                "metaDescription": "Beta.",
                "excerpt": "Beta.",
                "tags": ["whatsapp"],
                "editorialNote": "Fonte WABetaInfo — conteúdo em testes."
            }
            """);

        var classifier = BuildClassifier(provider);
        var item = BuildNormalizedItem(SourceType.BetaSpecialized);

        var result = await classifier.ClassifyAsync(item);

        Assert.Equal("Fonte WABetaInfo — conteúdo em testes.", result.EditorialNote);
    }

    [Fact]
    public async Task Classifier_HandlesIrrelevantItem()
    {
        var provider = new FakeProvider("""
            {
                "isRelevant": false,
                "discardReason": "Conteúdo não relacionado ao WhatsApp",
                "editorialType": "OfficialNews",
                "suggestedTitle": "",
                "slug": "",
                "metaTitle": "",
                "metaDescription": "",
                "excerpt": "",
                "tags": [],
                "editorialNote": null
            }
            """);

        var classifier = BuildClassifier(provider);
        var result = await classifier.ClassifyAsync(BuildNormalizedItem());

        Assert.False(result.IsRelevant);
        Assert.Equal("Conteúdo não relacionado ao WhatsApp", result.DiscardReason);
    }

    [Fact]
    public async Task Classifier_UsesClassificationModel()
    {
        var provider = new FakeProvider(
            """{"isRelevant":false,"discardReason":"test","editorialType":"OfficialNews","suggestedTitle":"","slug":"","metaTitle":"","metaDescription":"","excerpt":"","tags":[]}""");

        var classifier = BuildClassifier(provider);
        await classifier.ClassifyAsync(BuildNormalizedItem());

        Assert.Equal("gemini-2.5-flash-lite", provider.LastRequest!.Model);
        Assert.True(provider.LastRequest.JsonMode);
        Assert.NotNull(provider.LastRequest.SystemInstruction);
    }

    [Fact]
    public async Task Classifier_ThrowsOnProviderFailure()
    {
        var provider = FakeProvider.Failing("API error");
        var classifier = BuildClassifier(provider);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => classifier.ClassifyAsync(BuildNormalizedItem()));
        Assert.Contains("classification failed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Classifier_ThrowsOnInvalidJson()
    {
        var provider = new FakeProvider("this is not valid json");
        var classifier = BuildClassifier(provider);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => classifier.ClassifyAsync(BuildNormalizedItem()));
        Assert.Contains("parse", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Article generation tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Generator_ParsesValidOfficialResponse()
    {
        var provider = new FakeProvider("""
            {
                "title": "WhatsApp anuncia novidade para usuários brasileiros",
                "excerpt": "Resumo da novidade anunciada pelo WhatsApp.",
                "contentHtml": "<h2>O que mudou</h2><p>Detalhes da novidade.</p><h2>Impacto</h2><p>Para o Brasil.</p>",
                "betaDisclaimer": null
            }
            """);

        var generator = BuildGenerator(provider);
        var item = BuildNormalizedItem(SourceType.Official);
        var classification = BuildClassification(EditorialType.OfficialNews);

        var result = await generator.GenerateArticleAsync(item, classification);

        Assert.Equal("WhatsApp anuncia novidade para usuários brasileiros", result.Title);
        Assert.Contains("<h2>", result.ContentHtml);
        Assert.Null(result.BetaDisclaimer);
    }

    [Fact]
    public async Task Generator_EnforcesBetaDisclaimerWhenMissing()
    {
        var provider = new FakeProvider("""
            {
                "title": "WhatsApp testa recurso",
                "excerpt": "Novo teste detectado.",
                "contentHtml": "<h2>Teste</h2><p>Detalhes.</p>",
                "betaDisclaimer": null
            }
            """);

        var generator = BuildGenerator(provider);
        var item = BuildNormalizedItem(SourceType.BetaSpecialized);
        var classification = BuildClassification(EditorialType.BetaNews);

        var result = await generator.GenerateArticleAsync(item, classification);

        Assert.NotNull(result.BetaDisclaimer);
        Assert.Contains("WABetaInfo", result.BetaDisclaimer);
        Assert.Contains("testes", result.BetaDisclaimer);
    }

    [Fact]
    public async Task Generator_ClearsBetaDisclaimerForOfficialContent()
    {
        var provider = new FakeProvider("""
            {
                "title": "WhatsApp oficial",
                "excerpt": "Novidade oficial.",
                "contentHtml": "<p>Conteúdo oficial.</p>",
                "betaDisclaimer": "Incorretamente incluído pela IA"
            }
            """);

        var generator = BuildGenerator(provider);
        var item = BuildNormalizedItem(SourceType.Official);
        var classification = BuildClassification(EditorialType.OfficialNews);

        var result = await generator.GenerateArticleAsync(item, classification);

        Assert.Null(result.BetaDisclaimer);
    }

    [Fact]
    public async Task Generator_PreservesBetaDisclaimerWhenProvided()
    {
        var provider = new FakeProvider("""
            {
                "title": "Recurso beta",
                "excerpt": "Beta.",
                "contentHtml": "<p>Beta.</p>",
                "betaDisclaimer": "Este recurso está em fase de testes pelo WABetaInfo."
            }
            """);

        var generator = BuildGenerator(provider);
        var item = BuildNormalizedItem(SourceType.BetaSpecialized);
        var classification = BuildClassification(EditorialType.BetaNews);

        var result = await generator.GenerateArticleAsync(item, classification);

        Assert.Equal("Este recurso está em fase de testes pelo WABetaInfo.", result.BetaDisclaimer);
    }

    [Fact]
    public async Task Generator_UsesGenerationModel()
    {
        var provider = new FakeProvider(
            """{"title":"T","excerpt":"E","contentHtml":"<p>C</p>","betaDisclaimer":null}""");

        var generator = BuildGenerator(provider);
        await generator.GenerateArticleAsync(BuildNormalizedItem(), BuildClassification());

        Assert.Equal("gemini-2.5-flash", provider.LastRequest!.Model);
        Assert.True(provider.LastRequest.JsonMode);
        Assert.NotNull(provider.LastRequest.SystemInstruction);
    }

    [Fact]
    public async Task Generator_ThrowsOnProviderFailure()
    {
        var provider = FakeProvider.Failing("timeout");
        var generator = BuildGenerator(provider);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => generator.GenerateArticleAsync(BuildNormalizedItem(), BuildClassification()));
        Assert.Contains("generation failed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Generator_ThrowsOnInvalidJson()
    {
        var provider = new FakeProvider("<html>not json</html>");
        var generator = BuildGenerator(provider);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => generator.GenerateArticleAsync(BuildNormalizedItem(), BuildClassification()));
    }

    // -----------------------------------------------------------------------
    // Structured response parsing tests
    // -----------------------------------------------------------------------

    [Fact]
    public void ClassificationResult_DeserializesFromCamelCaseJson()
    {
        var json = """
            {
                "isRelevant": true,
                "discardReason": null,
                "editorialType": "OfficialNews",
                "suggestedTitle": "Título teste",
                "slug": "titulo-teste",
                "metaTitle": "Título teste SEO",
                "metaDescription": "Descrição SEO até 160 chars.",
                "excerpt": "Resumo curto.",
                "tags": ["whatsapp", "teste", "funcionalidade"],
                "editorialNote": "Nota editorial opcional."
            }
            """;

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
        var result = JsonSerializer.Deserialize<ClassificationResultDto>(json, options);

        Assert.NotNull(result);
        Assert.True(result.IsRelevant);
        Assert.Equal(EditorialType.OfficialNews, result.EditorialType);
        Assert.Equal("titulo-teste", result.Slug);
        Assert.Equal(3, result.Tags.Length);
        Assert.Equal("Nota editorial opcional.", result.EditorialNote);
    }

    [Fact]
    public void GeneratedArticle_DeserializesFromCamelCaseJson()
    {
        var json = """
            {
                "title": "Artigo completo em PT-BR",
                "excerpt": "Resumo do artigo gerado.",
                "contentHtml": "<h2>Seção principal</h2><p>Parágrafo detalhado.</p><h3>Subseção</h3><p>Mais detalhes.</p>",
                "betaDisclaimer": "Conteúdo em fase de testes."
            }
            """;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<GeneratedArticleDto>(json, options);

        Assert.NotNull(result);
        Assert.Equal("Artigo completo em PT-BR", result.Title);
        Assert.Contains("<h2>", result.ContentHtml);
        Assert.Contains("<h3>", result.ContentHtml);
        Assert.Equal("Conteúdo em fase de testes.", result.BetaDisclaimer);
    }

    [Fact]
    public void ClassificationResult_DeserializesWithMinimalFields()
    {
        var json = """{"isRelevant":false,"discardReason":"irrelevant","tags":[]}""";

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<ClassificationResultDto>(json, options);

        Assert.NotNull(result);
        Assert.False(result.IsRelevant);
        Assert.Equal("irrelevant", result.DiscardReason);
        Assert.Empty(result.Tags);
    }

    // -----------------------------------------------------------------------
    // Provider contract tests
    // -----------------------------------------------------------------------

    [Fact]
    public void TextGenerationRequest_HasCorrectDefaults()
    {
        var request = new TextGenerationRequest
        {
            Model = "test-model",
            Prompt = "test prompt"
        };

        Assert.False(request.JsonMode);
        Assert.Null(request.Temperature);
        Assert.Null(request.SystemInstruction);
    }

    [Fact]
    public void TextGenerationResponse_Success()
    {
        var response = new TextGenerationResponse
        {
            Success = true,
            Text = "result text",
            FinishReason = "STOP"
        };

        Assert.True(response.Success);
        Assert.Equal("result text", response.Text);
        Assert.Null(response.ErrorMessage);
    }

    [Fact]
    public void TextGenerationResponse_Failure()
    {
        var response = new TextGenerationResponse
        {
            Success = false,
            ErrorMessage = "API key invalid"
        };

        Assert.False(response.Success);
        Assert.Null(response.Text);
        Assert.Equal("API key invalid", response.ErrorMessage);
    }

    // -----------------------------------------------------------------------
    // OpenAI contract tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task OpenAiProvider_ThrowsNotImplemented()
    {
        var provider = new OpenAiTextGenerationProvider();
        var request = new TextGenerationRequest { Model = "gpt-4o", Prompt = "test" };

        await Assert.ThrowsAsync<NotImplementedException>(
            () => provider.GenerateAsync(request));
    }

    [Fact]
    public void OpenAiSettings_HasCorrectDefaults()
    {
        var settings = new OpenAiSettings();

        Assert.Equal("gpt-4o-mini", settings.ClassificationModel);
        Assert.Equal("gpt-4o", settings.GenerationModel);
        Assert.Equal("https://api.openai.com/v1", settings.BaseUrl);
        Assert.Equal(60, settings.TimeoutSeconds);
    }

    [Fact]
    public void GeminiSettings_HasCorrectDefaults()
    {
        var settings = new GeminiSettings();

        Assert.Equal("gemini-2.5-flash-lite", settings.ClassificationModel);
        Assert.Equal("gemini-2.5-flash", settings.GenerationModel);
        Assert.Contains("generativelanguage.googleapis.com", settings.BaseUrl);
        Assert.Equal(60, settings.TimeoutSeconds);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static GeminiClassifier BuildClassifier(ITextGenerationProvider provider)
    {
        var settings = Options.Create(new GeminiSettings());
        return new GeminiClassifier(provider, settings, NullLogger<GeminiClassifier>.Instance);
    }

    private static GeminiArticleGenerator BuildGenerator(ITextGenerationProvider provider)
    {
        var settings = Options.Create(new GeminiSettings());
        return new GeminiArticleGenerator(provider, settings, NullLogger<GeminiArticleGenerator>.Instance);
    }

    private static NormalizedItemDto BuildNormalizedItem(SourceType sourceType = SourceType.Official) =>
        new()
        {
            SourceItemId = Guid.NewGuid(),
            SourceId = Guid.NewGuid(),
            OriginalUrl = "https://blog.whatsapp.com/test-article",
            CanonicalUrl = "https://blog.whatsapp.com/test-article",
            Title = "WhatsApp announces new feature for video calls",
            NormalizedContent = "WhatsApp has announced a new feature that allows users to share screens during video calls. " +
                "The feature is rolling out to all users globally and will be available on both Android and iOS.",
            ContentHash = "sha256:abc123",
            PublishedAt = new DateTime(2026, 3, 28, 10, 0, 0, DateTimeKind.Utc),
            SourceType = sourceType
        };

    private static ClassificationResultDto BuildClassification(
        EditorialType type = EditorialType.OfficialNews) =>
        new()
        {
            IsRelevant = true,
            EditorialType = type,
            SuggestedTitle = "WhatsApp anuncia nova funcionalidade para videochamadas",
            Slug = "whatsapp-anuncia-nova-funcionalidade-videochamadas",
            MetaTitle = "WhatsApp anuncia nova funcionalidade para videochamadas",
            MetaDescription = "O WhatsApp anunciou oficialmente uma nova funcionalidade de compartilhamento de tela em videochamadas.",
            Excerpt = "Nova funcionalidade de compartilhamento de tela em videochamadas do WhatsApp.",
            Tags = ["whatsapp", "videochamada", "novidade"],
            EditorialNote = type == EditorialType.BetaNews
                ? "Conteúdo em fase de testes — fonte WABetaInfo." : null
        };

    // -----------------------------------------------------------------------
    // Fake provider
    // -----------------------------------------------------------------------

    private class FakeProvider : ITextGenerationProvider
    {
        private readonly string? _responseText;
        private readonly bool _shouldSucceed;
        private readonly string? _errorMessage;

        public TextGenerationRequest? LastRequest { get; private set; }

        public FakeProvider(string responseText)
        {
            _responseText = responseText;
            _shouldSucceed = true;
        }

        private FakeProvider(string? errorMessage, bool _)
        {
            _shouldSucceed = false;
            _errorMessage = errorMessage;
        }

        public static FakeProvider Failing(string errorMessage) => new(errorMessage, false);

        public Task<TextGenerationResponse> GenerateAsync(
            TextGenerationRequest request, CancellationToken ct = default)
        {
            LastRequest = request;
            return Task.FromResult(new TextGenerationResponse
            {
                Success = _shouldSucceed,
                Text = _shouldSucceed ? _responseText : null,
                ErrorMessage = _shouldSucceed ? null : (_errorMessage ?? "Mock error"),
                FinishReason = _shouldSucceed ? "STOP" : null
            });
        }
    }
}
