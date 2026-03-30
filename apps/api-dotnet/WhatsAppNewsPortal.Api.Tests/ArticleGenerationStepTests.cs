using Microsoft.Extensions.Logging.Abstractions;
using WhatsAppNewsPortal.Api.AiGeneration.Application;
using WhatsAppNewsPortal.Api.Articles.Application;
using WhatsAppNewsPortal.Api.Articles.Domain;
using WhatsAppNewsPortal.Api.Articles.Infrastructure;
using WhatsAppNewsPortal.Api.Common;
using WhatsAppNewsPortal.Api.ContentProcessing.Application;
using WhatsAppNewsPortal.Api.Sources.Application;
using WhatsAppNewsPortal.Api.Sources.Domain;

namespace WhatsAppNewsPortal.Api.Tests;

public class ArticleGenerationStepTests
{
    // =====================================================================
    // Golden tests — realistic end-to-end scenarios
    // =====================================================================

    [Fact]
    public async Task Golden_OfficialArticle_CreatesDraftWithAllMetadata()
    {
        var item = Fixtures.OfficialItem();
        var classification = Fixtures.OfficialClassification();
        var generated = Fixtures.OfficialGeneratedArticle();

        var generator = new FakeGenerator(generated);
        var articleRepo = new FakeArticleRepo();
        var sourceItemRepo = new FakeSourceItemRepo(item.SourceItemId);
        var logRepo = new FakeLogRepo();

        var step = BuildStep(generator, articleRepo, sourceItemRepo, logRepo);
        var result = await step.ExecuteAsync(item, classification);

        Assert.True(result.Success);
        Assert.NotNull(result.ArticleId);
        Assert.Equal("whatsapp-lanca-compartilhamento-de-tela", result.Slug);

        // Article persisted as draft with correct fields
        var article = articleRepo.Added!;
        Assert.Equal(PipelineStatus.Draft, article.Status);
        Assert.Equal(item.SourceItemId, article.SourceItemId);
        Assert.Equal(generated.Title, article.Title);
        Assert.Equal(generated.Excerpt, article.Excerpt);
        Assert.Equal(generated.ContentHtml, article.ContentHtml);
        Assert.Equal(classification.MetaTitle, article.MetaTitle);
        Assert.Equal(classification.MetaDescription, article.MetaDescription);
        Assert.Equal(classification.Tags, article.Tags);
        Assert.Equal(EditorialType.OfficialNews, article.ArticleType);
        Assert.Equal("oficial", article.Category);
        Assert.Null(article.PublishedAt);

        // No beta disclaimer in content
        Assert.DoesNotContain("beta-disclaimer", article.ContentHtml);

        // SourceItem updated to Draft
        Assert.Equal(PipelineStatus.Draft, sourceItemRepo.LastUpdatedItem!.Status);

        // Log created
        Assert.Single(logRepo.Logs);
        Assert.Equal("ArticleGeneration", logRepo.Logs[0].StepName);
        Assert.Equal("success", logRepo.Logs[0].Status);
    }

    [Fact]
    public async Task Golden_BetaArticle_CreatesDraftWithDisclaimer()
    {
        var item = Fixtures.BetaItem();
        var classification = Fixtures.BetaClassification();
        var generated = Fixtures.BetaGeneratedArticle();

        var generator = new FakeGenerator(generated);
        var articleRepo = new FakeArticleRepo();
        var sourceItemRepo = new FakeSourceItemRepo(item.SourceItemId, "WABetaInfo");
        var logRepo = new FakeLogRepo();

        var step = BuildStep(generator, articleRepo, sourceItemRepo, logRepo);
        var result = await step.ExecuteAsync(item, classification);

        Assert.True(result.Success);
        Assert.NotNull(result.ArticleId);

        var article = articleRepo.Added!;
        Assert.Equal(EditorialType.BetaNews, article.ArticleType);
        Assert.Equal("beta", article.Category);

        // Beta disclaimer prepended to content
        Assert.Contains("beta-disclaimer", article.ContentHtml);
        Assert.Contains("WABetaInfo", article.ContentHtml);
        Assert.StartsWith("<aside", article.ContentHtml);

        // Original content still present after disclaimer
        Assert.Contains("<h2>", article.ContentHtml);

        Assert.Equal(PipelineStatus.Draft, sourceItemRepo.LastUpdatedItem!.Status);
        Assert.Single(logRepo.Logs);
        Assert.Equal("success", logRepo.Logs[0].Status);
    }

    [Fact]
    public async Task Golden_OfficialArticle_PersistsSourceReference()
    {
        var item = Fixtures.OfficialItem();
        var classification = Fixtures.OfficialClassification();
        var generated = Fixtures.OfficialGeneratedArticle();

        var generator = new FakeGenerator(generated);
        var articleRepo = new FakeArticleRepo();
        var sourceItemRepo = new FakeSourceItemRepo(item.SourceItemId, "WhatsApp Blog");

        var step = BuildStep(generator, articleRepo, sourceItemRepo);
        var result = await step.ExecuteAsync(item, classification);

        Assert.True(result.Success);

        var article = articleRepo.Added!;
        Assert.Single(article.SourceReferences);

        var reference = article.SourceReferences.First();
        Assert.Equal(article.Id, reference.ArticleId);
        Assert.Equal("WhatsApp Blog", reference.SourceName);
        Assert.Equal(item.OriginalUrl, reference.SourceUrl);
        Assert.Equal("primary", reference.ReferenceType);
        Assert.NotEqual(Guid.Empty, reference.Id);
    }

    [Fact]
    public async Task Golden_BetaArticle_PersistsSourceReference()
    {
        var item = Fixtures.BetaItem();
        var classification = Fixtures.BetaClassification();
        var generated = Fixtures.BetaGeneratedArticle();

        var generator = new FakeGenerator(generated);
        var articleRepo = new FakeArticleRepo();
        var sourceItemRepo = new FakeSourceItemRepo(item.SourceItemId, "WABetaInfo");

        var step = BuildStep(generator, articleRepo, sourceItemRepo);
        var result = await step.ExecuteAsync(item, classification);

        Assert.True(result.Success);

        var article = articleRepo.Added!;
        Assert.Single(article.SourceReferences);

        var reference = article.SourceReferences.First();
        Assert.Equal("WABetaInfo", reference.SourceName);
        Assert.Equal(item.OriginalUrl, reference.SourceUrl);
        Assert.Equal("primary", reference.ReferenceType);
    }

    [Fact]
    public async Task DraftHasNoEssentialFieldsMissing()
    {
        var item = Fixtures.OfficialItem();
        var classification = Fixtures.OfficialClassification();
        var generated = Fixtures.OfficialGeneratedArticle();

        var generator = new FakeGenerator(generated);
        var articleRepo = new FakeArticleRepo();
        var sourceItemRepo = new FakeSourceItemRepo(item.SourceItemId);

        var step = BuildStep(generator, articleRepo, sourceItemRepo);
        var result = await step.ExecuteAsync(item, classification);

        Assert.True(result.Success);

        var article = articleRepo.Added!;

        // All essential fields must be populated
        Assert.NotEqual(Guid.Empty, article.Id);
        Assert.NotEqual(Guid.Empty, article.SourceItemId);
        Assert.Equal(item.SourceItemId, article.SourceItemId);
        Assert.False(string.IsNullOrWhiteSpace(article.Slug));
        Assert.False(string.IsNullOrWhiteSpace(article.Title));
        Assert.False(string.IsNullOrWhiteSpace(article.Excerpt));
        Assert.False(string.IsNullOrWhiteSpace(article.ContentHtml));
        Assert.False(string.IsNullOrWhiteSpace(article.MetaTitle));
        Assert.False(string.IsNullOrWhiteSpace(article.MetaDescription));
        Assert.NotEmpty(article.Tags);
        Assert.False(string.IsNullOrWhiteSpace(article.Category));
        Assert.Equal(PipelineStatus.Draft, article.Status);
        Assert.Equal(classification.EditorialType, article.ArticleType);
        Assert.Null(article.PublishedAt);

        // Source reference must be present
        Assert.NotEmpty(article.SourceReferences);
    }

    [Fact]
    public async Task SourceNameFallsBackToHostWhenSourceNotFound()
    {
        var item = Fixtures.OfficialItem();
        var classification = Fixtures.OfficialClassification();
        var generated = Fixtures.OfficialGeneratedArticle();

        var generator = new FakeGenerator(generated);
        var articleRepo = new FakeArticleRepo();
        // Pass null sourceItemId so GetByIdAsync returns null
        var sourceItemRepo = new FakeSourceItemRepo(null);

        var step = BuildStep(generator, articleRepo, sourceItemRepo);
        var result = await step.ExecuteAsync(item, classification);

        Assert.True(result.Success);

        var article = articleRepo.Added!;
        Assert.Single(article.SourceReferences);

        var reference = article.SourceReferences.First();
        Assert.Equal("blog.whatsapp.com", reference.SourceName);
        Assert.Equal(item.OriginalUrl, reference.SourceUrl);
    }

    [Fact]
    public async Task Golden_OfficialArticle_NoBetaDisclaimerInHtml()
    {
        var item = Fixtures.OfficialItem();
        var classification = Fixtures.OfficialClassification();
        var generated = Fixtures.OfficialGeneratedArticle();

        var generator = new FakeGenerator(generated);
        var articleRepo = new FakeArticleRepo();

        var step = BuildStep(generator, articleRepo, new FakeSourceItemRepo(item.SourceItemId));
        var result = await step.ExecuteAsync(item, classification);

        Assert.True(result.Success);
        Assert.DoesNotContain("beta-disclaimer", articleRepo.Added!.ContentHtml);
        Assert.DoesNotContain("Atenção:", articleRepo.Added.ContentHtml);
    }

    // =====================================================================
    // Validation tests
    // =====================================================================

    [Fact]
    public async Task Validation_MissingTitle_Fails()
    {
        var generated = Fixtures.OfficialGeneratedArticle();
        generated.Title = "";

        var result = await RunStep(generated, Fixtures.OfficialClassification());

        Assert.False(result.Success);
        Assert.Contains(result.ValidationErrors, e => e.Contains("Title"));
    }

    [Fact]
    public async Task Validation_MissingExcerpt_Fails()
    {
        var generated = Fixtures.OfficialGeneratedArticle();
        generated.Excerpt = "";

        var result = await RunStep(generated, Fixtures.OfficialClassification());

        Assert.False(result.Success);
        Assert.Contains(result.ValidationErrors, e => e.Contains("Excerpt"));
    }

    [Fact]
    public async Task Validation_MissingContentHtml_Fails()
    {
        var generated = Fixtures.OfficialGeneratedArticle();
        generated.ContentHtml = "";

        var result = await RunStep(generated, Fixtures.OfficialClassification());

        Assert.False(result.Success);
        Assert.Contains(result.ValidationErrors, e => e.Contains("ContentHtml"));
    }

    [Fact]
    public async Task Validation_ContentWithoutHtmlTags_Fails()
    {
        var generated = Fixtures.OfficialGeneratedArticle();
        generated.ContentHtml = "Texto sem nenhuma tag HTML";

        var result = await RunStep(generated, Fixtures.OfficialClassification());

        Assert.False(result.Success);
        Assert.Contains(result.ValidationErrors, e => e.Contains("HTML"));
    }

    [Fact]
    public async Task Validation_BetaArticleMissingDisclaimer_Fails()
    {
        var generated = Fixtures.BetaGeneratedArticle();
        generated.BetaDisclaimer = null;

        var result = await RunStep(generated, Fixtures.BetaClassification());

        Assert.False(result.Success);
        Assert.Contains(result.ValidationErrors, e => e.Contains("BetaDisclaimer"));
    }

    [Fact]
    public async Task Validation_MultipleErrors_AllReported()
    {
        var generated = new GeneratedArticleDto
        {
            Title = "",
            Excerpt = "",
            ContentHtml = "",
            BetaDisclaimer = null
        };

        var result = await RunStep(generated, Fixtures.BetaClassification());

        Assert.False(result.Success);
        Assert.True(result.ValidationErrors.Count >= 3);
    }

    [Fact]
    public async Task Validation_Failure_SetsSourceItemToFailed()
    {
        var item = Fixtures.OfficialItem();
        var generated = Fixtures.OfficialGeneratedArticle();
        generated.Title = "";

        var generator = new FakeGenerator(generated);
        var sourceItemRepo = new FakeSourceItemRepo(item.SourceItemId);
        var logRepo = new FakeLogRepo();

        var step = BuildStep(generator, new FakeArticleRepo(), sourceItemRepo, logRepo);
        await step.ExecuteAsync(item, Fixtures.OfficialClassification());

        Assert.Equal(PipelineStatus.Failed, sourceItemRepo.LastUpdatedItem!.Status);
        Assert.Single(logRepo.Logs);
        Assert.Equal("failure", logRepo.Logs[0].Status);
    }

    // =====================================================================
    // Provider mock tests
    // =====================================================================

    [Fact]
    public async Task Generator_Throws_StepReturnsFailed()
    {
        var item = Fixtures.OfficialItem();
        var generator = FakeGenerator.Throwing(
            new InvalidOperationException("Gemini API timeout"));
        var sourceItemRepo = new FakeSourceItemRepo(item.SourceItemId);
        var logRepo = new FakeLogRepo();

        var step = BuildStep(generator, new FakeArticleRepo(), sourceItemRepo, logRepo);
        var result = await step.ExecuteAsync(item, Fixtures.OfficialClassification());

        Assert.False(result.Success);
        Assert.Contains("Gemini API timeout", result.ErrorMessage);
        Assert.Equal(PipelineStatus.Failed, sourceItemRepo.LastUpdatedItem!.Status);
        Assert.Single(logRepo.Logs);
        Assert.Equal("failure", logRepo.Logs[0].Status);
    }

    [Fact]
    public async Task DuplicateArticle_ReturnsFailedWithoutCallingGenerator()
    {
        var item = Fixtures.OfficialItem();
        var generator = new FakeGenerator(Fixtures.OfficialGeneratedArticle());
        var articleRepo = new FakeArticleRepo
        {
            ExistingSourceItemIds = { item.SourceItemId }
        };

        var step = BuildStep(generator, articleRepo, new FakeSourceItemRepo(item.SourceItemId));
        var result = await step.ExecuteAsync(item, Fixtures.OfficialClassification());

        Assert.False(result.Success);
        Assert.Contains("already exists", result.ErrorMessage);
        Assert.False(generator.WasCalled);
    }

    // =====================================================================
    // Slug deduplication tests
    // =====================================================================

    [Fact]
    public async Task SlugConflict_AppendsSuffix()
    {
        var item = Fixtures.OfficialItem();
        var classification = Fixtures.OfficialClassification();
        var generated = Fixtures.OfficialGeneratedArticle();

        var articleRepo = new FakeArticleRepo
        {
            ExistingSlugs = { classification.Slug }
        };

        var step = BuildStep(new FakeGenerator(generated), articleRepo,
            new FakeSourceItemRepo(item.SourceItemId));
        var result = await step.ExecuteAsync(item, classification);

        Assert.True(result.Success);
        Assert.Equal($"{classification.Slug}-2", result.Slug);
    }

    [Fact]
    public async Task MultipleSlugConflicts_IncrementsSuffix()
    {
        var item = Fixtures.OfficialItem();
        var classification = Fixtures.OfficialClassification();
        var generated = Fixtures.OfficialGeneratedArticle();

        var articleRepo = new FakeArticleRepo
        {
            ExistingSlugs =
            {
                classification.Slug,
                $"{classification.Slug}-2"
            }
        };

        var step = BuildStep(new FakeGenerator(generated), articleRepo,
            new FakeSourceItemRepo(item.SourceItemId));
        var result = await step.ExecuteAsync(item, classification);

        Assert.True(result.Success);
        Assert.Equal($"{classification.Slug}-3", result.Slug);
    }

    // =====================================================================
    // ArticleValidator — direct unit tests
    // =====================================================================

    [Fact]
    public void Validator_ValidOfficialArticle_NoErrors()
    {
        var errors = ArticleValidator.Validate(
            Fixtures.OfficialGeneratedArticle(), EditorialType.OfficialNews);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validator_ValidBetaArticle_NoErrors()
    {
        var errors = ArticleValidator.Validate(
            Fixtures.BetaGeneratedArticle(), EditorialType.BetaNews);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validator_PlainTextContent_ReportsError()
    {
        var article = Fixtures.OfficialGeneratedArticle();
        article.ContentHtml = "Apenas texto sem tags";

        var errors = ArticleValidator.Validate(article, EditorialType.OfficialNews);
        Assert.Single(errors);
        Assert.Contains("HTML", errors[0]);
    }

    [Fact]
    public void Sanitizer_TrimsWhitespace()
    {
        var article = new GeneratedArticleDto
        {
            Title = "  Título  ",
            Excerpt = "  Resumo  ",
            ContentHtml = "  <p>Conteúdo</p>  ",
            BetaDisclaimer = "  Aviso  "
        };

        ArticleValidator.Sanitize(article);

        Assert.Equal("Título", article.Title);
        Assert.Equal("Resumo", article.Excerpt);
        Assert.Equal("<p>Conteúdo</p>", article.ContentHtml);
        Assert.Equal("Aviso", article.BetaDisclaimer);
    }

    [Fact]
    public void Sanitizer_EmptyDisclaimer_BecomesNull()
    {
        var article = new GeneratedArticleDto
        {
            Title = "T",
            Excerpt = "E",
            ContentHtml = "<p>C</p>",
            BetaDisclaimer = "   "
        };

        ArticleValidator.Sanitize(article);

        Assert.Null(article.BetaDisclaimer);
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    private async Task<ArticleGenerationStepResult> RunStep(
        GeneratedArticleDto generated, ClassificationResultDto classification)
    {
        var item = Fixtures.OfficialItem();
        var step = BuildStep(
            new FakeGenerator(generated),
            new FakeArticleRepo(),
            new FakeSourceItemRepo(item.SourceItemId));
        return await step.ExecuteAsync(item, classification);
    }

    private static ArticleGenerationStep BuildStep(
        IAiArticleGenerator generator,
        IArticleRepository articleRepo,
        ISourceItemRepository sourceItemRepo,
        IProcessingLogRepository? logRepo = null)
    {
        return new ArticleGenerationStep(
            generator,
            articleRepo,
            sourceItemRepo,
            logRepo ?? new FakeLogRepo(),
            NullLogger<ArticleGenerationStep>.Instance);
    }

    // =====================================================================
    // Fixtures
    // =====================================================================

    private static class Fixtures
    {
        public static NormalizedItemDto OfficialItem() => new()
        {
            SourceItemId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
            SourceId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            OriginalUrl = "https://blog.whatsapp.com/screen-sharing-video-calls",
            CanonicalUrl = "https://blog.whatsapp.com/screen-sharing-video-calls",
            Title = "Screen Sharing on Video Calls",
            NormalizedContent =
                "Today we're launching screen sharing on WhatsApp video calls. " +
                "You can now share your screen during a video call on WhatsApp, making it easier to " +
                "browse the web together, shop with friends, or show photos to family. " +
                "The feature is available on Android and iOS starting today.",
            ContentHash = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4",
            PublishedAt = new DateTime(2026, 3, 25, 14, 0, 0, DateTimeKind.Utc),
            SourceType = SourceType.Official
        };

        public static NormalizedItemDto BetaItem() => new()
        {
            SourceItemId = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901"),
            SourceId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            OriginalUrl = "https://wabetainfo.com/voice-transcription-beta",
            CanonicalUrl = "https://wabetainfo.com/voice-transcription-beta",
            Title = "WhatsApp is working on voice message transcription",
            NormalizedContent =
                "WABetaInfo has discovered that WhatsApp is developing automatic voice message " +
                "transcription. The feature was spotted in WhatsApp beta for Android version 2.26.5.12. " +
                "Transcription is processed locally on device for privacy.",
            ContentHash = "b2c3d4e5f6a7b2c3d4e5f6a7b2c3d4e5",
            PublishedAt = new DateTime(2026, 3, 27, 9, 30, 0, DateTimeKind.Utc),
            SourceType = SourceType.BetaSpecialized
        };

        public static ClassificationResultDto OfficialClassification() => new()
        {
            IsRelevant = true,
            EditorialType = EditorialType.OfficialNews,
            SuggestedTitle = "WhatsApp lança compartilhamento de tela em videochamadas",
            Slug = "whatsapp-lanca-compartilhamento-de-tela",
            MetaTitle = "WhatsApp lança compartilhamento de tela em videochamadas",
            MetaDescription = "O WhatsApp lançou oficialmente o compartilhamento de tela durante videochamadas.",
            Excerpt = "Recurso de compartilhamento de tela em videochamadas do WhatsApp.",
            Tags = ["whatsapp", "videochamada", "compartilhamento-de-tela"],
            EditorialNote = null
        };

        public static ClassificationResultDto BetaClassification() => new()
        {
            IsRelevant = true,
            EditorialType = EditorialType.BetaNews,
            SuggestedTitle = "WhatsApp testa transcrição automática de áudio",
            Slug = "whatsapp-testa-transcricao-automatica-audio",
            MetaTitle = "WhatsApp testa transcrição de áudio em versão beta",
            MetaDescription = "WABetaInfo descobriu transcrição automática de mensagens de voz no WhatsApp beta.",
            Excerpt = "Transcrição automática de mensagens de voz em desenvolvimento no WhatsApp.",
            Tags = ["whatsapp", "beta", "transcricao", "wabetainfo"],
            EditorialNote = "Conteúdo do WABetaInfo — funcionalidade em fase de testes."
        };

        public static GeneratedArticleDto OfficialGeneratedArticle() => new()
        {
            Title = "WhatsApp lança compartilhamento de tela em videochamadas para todos os usuários",
            Excerpt = "O WhatsApp anunciou oficialmente o recurso de compartilhamento de tela durante " +
                "videochamadas, já disponível para Android e iOS.",
            ContentHtml =
                "<h2>O que é o novo recurso</h2>" +
                "<p>O WhatsApp lançou oficialmente o compartilhamento de tela durante videochamadas. " +
                "Com essa funcionalidade, os usuários podem mostrar o conteúdo de seus dispositivos " +
                "em tempo real durante uma chamada de vídeo.</p>" +
                "<h2>Como funciona na prática</h2>" +
                "<p>Para usar, basta iniciar uma videochamada, tocar no ícone de compartilhamento " +
                "e selecionar \"Compartilhar tela\". O recurso funciona tanto em chamadas individuais " +
                "quanto em grupos com até 32 participantes.</p>" +
                "<h3>Compatibilidade</h3>" +
                "<p>O recurso está disponível para Android e iOS a partir de hoje.</p>" +
                "<h2>Impacto para o usuário brasileiro</h2>" +
                "<p>Para os usuários brasileiros, essa funcionalidade facilita desde apresentações " +
                "de trabalho remotas até o compartilhamento de fotos com familiares à distância.</p>",
            BetaDisclaimer = null
        };

        public static GeneratedArticleDto BetaGeneratedArticle() => new()
        {
            Title = "WhatsApp testa transcrição automática de mensagens de voz em versão beta",
            Excerpt = "O WABetaInfo identificou que o WhatsApp está desenvolvendo transcrição " +
                "automática de mensagens de voz com processamento local no dispositivo.",
            ContentHtml =
                "<h2>O que foi descoberto</h2>" +
                "<p>O WABetaInfo identificou uma nova funcionalidade em desenvolvimento no WhatsApp: " +
                "a transcrição automática de mensagens de voz. O recurso foi encontrado na versão " +
                "beta para Android 2.26.5.12.</p>" +
                "<h2>Como deve funcionar</h2>" +
                "<p>Quando disponível, os usuários verão um botão \"Transcrever\" abaixo das mensagens " +
                "de voz. A transcrição é processada localmente no dispositivo, garantindo privacidade.</p>" +
                "<h3>Idiomas suportados</h3>" +
                "<p>A funcionalidade deverá suportar português, inglês e espanhol.</p>" +
                "<h2>Impacto para usuários brasileiros</h2>" +
                "<p>Para o público brasileiro, a transcrição pode facilitar o uso do WhatsApp em " +
                "ambientes onde não é possível ouvir áudio.</p>",
            BetaDisclaimer = "Este conteúdo é baseado em informações do WABetaInfo e refere-se a " +
                "funcionalidades ainda em fase de testes, sem confirmação oficial do WhatsApp."
        };
    }

    // =====================================================================
    // Fakes
    // =====================================================================

    private class FakeGenerator : IAiArticleGenerator
    {
        private readonly GeneratedArticleDto? _result;
        private readonly Exception? _exception;
        public bool WasCalled { get; private set; }

        public FakeGenerator(GeneratedArticleDto result) => _result = result;
        private FakeGenerator(Exception exception) => _exception = exception;

        public static FakeGenerator Throwing(Exception ex) => new(ex);

        public Task<GeneratedArticleDto> GenerateArticleAsync(
            NormalizedItemDto item, ClassificationResultDto classification,
            CancellationToken ct = default)
        {
            WasCalled = true;
            if (_exception is not null) throw _exception;
            return Task.FromResult(_result!);
        }
    }

    private class FakeArticleRepo : IArticleRepository
    {
        public HashSet<Guid> ExistingSourceItemIds { get; } = [];
        public HashSet<string> ExistingSlugs { get; } = [];
        public Article? Added { get; private set; }

        public Task<Article?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<Article?>(null);

        public Task<Article?> GetBySlugAsync(string slug, CancellationToken ct = default)
            => Task.FromResult<Article?>(
                ExistingSlugs.Contains(slug)
                    ? new Article { Slug = slug }
                    : null);

        public Task<List<Article>> GetPublishedAsync(int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(new List<Article>());

        public Task<List<Article>> GetByCategoryAsync(string category, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(new List<Article>());
        public Task<List<Article>> GetByTopicAsync(string topic, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(new List<Article>());

        public Task<bool> ExistsBySourceItemIdAsync(Guid sourceItemId, CancellationToken ct = default)
            => Task.FromResult(ExistingSourceItemIds.Contains(sourceItemId));

        public Task AddAsync(Article article, CancellationToken ct = default)
        {
            Added = article;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Article article, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private class FakeSourceItemRepo : ISourceItemRepository
    {
        private readonly Guid? _existingId;
        private readonly string _sourceName;
        public SourceItem? LastUpdatedItem { get; private set; }

        public FakeSourceItemRepo(Guid? sourceItemId, string sourceName = "WhatsApp Blog")
        {
            _existingId = sourceItemId;
            _sourceName = sourceName;
        }

        public Task<SourceItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            if (_existingId == null || id != _existingId)
                return Task.FromResult<SourceItem?>(null);
            var item = LastUpdatedItem ?? new SourceItem
            {
                Id = id, SourceId = Guid.NewGuid(),
                OriginalUrl = "https://example.com", Title = "Test",
                Status = PipelineStatus.Processing,
                Source = new Source
                {
                    Id = Guid.NewGuid(),
                    Name = _sourceName,
                    Type = SourceType.Official,
                    BaseUrl = "https://blog.whatsapp.com"
                }
            };
            return Task.FromResult<SourceItem?>(item);
        }

        public Task<bool> ExistsByUrlAsync(string url, CancellationToken ct = default)
            => Task.FromResult(false);
        public Task<bool> ExistsByCanonicalUrlAsync(string url, CancellationToken ct = default)
            => Task.FromResult(false);
        public Task<bool> ExistsByContentHashAsync(string hash, CancellationToken ct = default)
            => Task.FromResult(false);
        public Task AddAsync(SourceItem item, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task UpdateAsync(SourceItem item, CancellationToken ct = default)
        {
            LastUpdatedItem = item;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(SourceItem item, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private class FakeLogRepo : IProcessingLogRepository
    {
        public List<ProcessingLog> Logs { get; } = [];
        public Task AddAsync(ProcessingLog log, CancellationToken ct = default)
        {
            Logs.Add(log);
            return Task.CompletedTask;
        }
    }
}
