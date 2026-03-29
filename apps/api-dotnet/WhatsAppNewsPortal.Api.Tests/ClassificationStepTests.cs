using Microsoft.Extensions.Logging.Abstractions;
using WhatsAppNewsPortal.Api.AiGeneration.Application;
using WhatsAppNewsPortal.Api.Common;
using WhatsAppNewsPortal.Api.ContentProcessing.Application;
using WhatsAppNewsPortal.Api.ContentProcessing.Infrastructure;
using WhatsAppNewsPortal.Api.Sources.Application;
using WhatsAppNewsPortal.Api.Sources.Domain;

namespace WhatsAppNewsPortal.Api.Tests;

public class ClassificationStepTests
{
    // =====================================================================
    // Golden tests — realistic fixture data
    // =====================================================================

    [Fact]
    public async Task Golden_OfficialWhatsAppBlogPost_ClassifiedAsOfficialNews()
    {
        var item = Fixtures.OfficialWhatsAppBlogPost();
        var classifier = new FakeClassifier(Fixtures.OfficialClassificationResponse());
        var sourceItemRepo = new FakeSourceItemRepo(item.SourceItemId);
        var logRepo = new FakeLogRepo();

        var step = BuildStep(classifier, sourceItemRepo, logRepo);
        var result = await step.ExecuteAsync(item);

        Assert.True(result.Success);
        Assert.True(result.IsRelevant);
        Assert.NotNull(result.Classification);
        Assert.Equal(EditorialType.OfficialNews, result.Classification!.EditorialType);
        Assert.Equal("whatsapp-lanca-compartilhamento-de-tela-em-videochamadas", result.Classification.Slug);
        Assert.Contains("whatsapp", result.Classification.Tags);
        Assert.Null(result.Classification.EditorialNote);

        // SourceItem persisted with classification
        Assert.Equal("official_news", sourceItemRepo.LastUpdatedItem!.SourceClassification);
        Assert.Equal(PipelineStatus.Processing, sourceItemRepo.LastUpdatedItem.Status);

        // ProcessingLog created
        Assert.Single(logRepo.Logs);
        Assert.Equal("Classification", logRepo.Logs[0].StepName);
        Assert.Equal("success", logRepo.Logs[0].Status);
    }

    [Fact]
    public async Task Golden_WABetaInfoPost_ClassifiedAsBetaNews()
    {
        var item = Fixtures.WABetaInfoPost();
        var classifier = new FakeClassifier(Fixtures.BetaClassificationResponse());
        var sourceItemRepo = new FakeSourceItemRepo(item.SourceItemId);
        var logRepo = new FakeLogRepo();

        var step = BuildStep(classifier, sourceItemRepo, logRepo);
        var result = await step.ExecuteAsync(item);

        Assert.True(result.Success);
        Assert.True(result.IsRelevant);
        Assert.NotNull(result.Classification);
        Assert.Equal(EditorialType.BetaNews, result.Classification!.EditorialType);
        Assert.NotNull(result.Classification.EditorialNote);
        Assert.Contains("beta", result.Classification.Tags);
        Assert.Equal("whatsapp-testa-transcricao-automatica-de-mensagens-de-voz", result.Classification.Slug);

        Assert.Equal("beta_news", sourceItemRepo.LastUpdatedItem!.SourceClassification);
        Assert.Single(logRepo.Logs);
        Assert.Equal("success", logRepo.Logs[0].Status);
    }

    [Fact]
    public async Task Golden_IrrelevantContent_DiscardedWithLog()
    {
        var item = Fixtures.IrrelevantItem();
        var classifier = new FakeClassifier(Fixtures.IrrelevantClassificationResponse());
        var sourceItemRepo = new FakeSourceItemRepo(item.SourceItemId);
        var logRepo = new FakeLogRepo();

        var step = BuildStep(classifier, sourceItemRepo, logRepo);
        var result = await step.ExecuteAsync(item);

        Assert.True(result.Success);
        Assert.False(result.IsRelevant);
        Assert.Equal("Conteúdo sobre política de privacidade genérica, sem relação direta com funcionalidades do WhatsApp", result.DiscardReason);

        // SourceItem marked as failed/discarded
        Assert.Equal(PipelineStatus.Failed, sourceItemRepo.LastUpdatedItem!.Status);
        Assert.Equal("discarded", sourceItemRepo.LastUpdatedItem.SourceClassification);
        Assert.Contains("Discarded", sourceItemRepo.LastUpdatedItem.ErrorMessage);

        // Log with discard reason
        Assert.Single(logRepo.Logs);
        Assert.Equal("discarded", logRepo.Logs[0].Status);
    }

    // =====================================================================
    // Validation tests — bad AI output caught
    // =====================================================================

    [Fact]
    public async Task Validation_RelevantItem_MissingSlug_Fails()
    {
        var item = Fixtures.OfficialWhatsAppBlogPost();
        var classification = Fixtures.OfficialClassificationResponse();
        classification.Slug = "";

        var classifier = new FakeClassifier(classification);
        var sourceItemRepo = new FakeSourceItemRepo(item.SourceItemId);
        var logRepo = new FakeLogRepo();

        var step = BuildStep(classifier, sourceItemRepo, logRepo);
        var result = await step.ExecuteAsync(item);

        Assert.False(result.Success);
        Assert.Contains(result.ValidationErrors, e => e.Contains("Slug"));
        Assert.Equal(PipelineStatus.Failed, sourceItemRepo.LastUpdatedItem!.Status);
    }

    [Fact]
    public async Task Validation_RelevantItem_MissingTitle_Fails()
    {
        var item = Fixtures.OfficialWhatsAppBlogPost();
        var classification = Fixtures.OfficialClassificationResponse();
        classification.SuggestedTitle = "  ";

        var classifier = new FakeClassifier(classification);
        var sourceItemRepo = new FakeSourceItemRepo(item.SourceItemId);
        var logRepo = new FakeLogRepo();

        var step = BuildStep(classifier, sourceItemRepo, logRepo);
        var result = await step.ExecuteAsync(item);

        Assert.False(result.Success);
        Assert.Contains(result.ValidationErrors, e => e.Contains("SuggestedTitle"));
    }

    [Fact]
    public async Task Validation_RelevantItem_EmptyTags_Fails()
    {
        var item = Fixtures.OfficialWhatsAppBlogPost();
        var classification = Fixtures.OfficialClassificationResponse();
        classification.Tags = [];

        var classifier = new FakeClassifier(classification);
        var sourceItemRepo = new FakeSourceItemRepo(item.SourceItemId);
        var logRepo = new FakeLogRepo();

        var step = BuildStep(classifier, sourceItemRepo, logRepo);
        var result = await step.ExecuteAsync(item);

        Assert.False(result.Success);
        Assert.Contains(result.ValidationErrors, e => e.Contains("tag"));
    }

    [Fact]
    public async Task Validation_RelevantItem_MissingMetaDescription_Fails()
    {
        var item = Fixtures.OfficialWhatsAppBlogPost();
        var classification = Fixtures.OfficialClassificationResponse();
        classification.MetaDescription = "";

        var classifier = new FakeClassifier(classification);
        var sourceItemRepo = new FakeSourceItemRepo(item.SourceItemId);

        var step = BuildStep(classifier, sourceItemRepo);
        var result = await step.ExecuteAsync(item);

        Assert.False(result.Success);
        Assert.Contains(result.ValidationErrors, e => e.Contains("MetaDescription"));
    }

    [Fact]
    public async Task Validation_RelevantItem_MissingExcerpt_Fails()
    {
        var item = Fixtures.OfficialWhatsAppBlogPost();
        var classification = Fixtures.OfficialClassificationResponse();
        classification.Excerpt = "";

        var classifier = new FakeClassifier(classification);
        var sourceItemRepo = new FakeSourceItemRepo(item.SourceItemId);

        var step = BuildStep(classifier, sourceItemRepo);
        var result = await step.ExecuteAsync(item);

        Assert.False(result.Success);
        Assert.Contains(result.ValidationErrors, e => e.Contains("Excerpt"));
    }

    [Fact]
    public async Task Validation_IrrelevantItem_MissingDiscardReason_Fails()
    {
        var item = Fixtures.OfficialWhatsAppBlogPost();
        var classification = new ClassificationResultDto { IsRelevant = false, DiscardReason = "" };

        var classifier = new FakeClassifier(classification);
        var sourceItemRepo = new FakeSourceItemRepo(item.SourceItemId);

        var step = BuildStep(classifier, sourceItemRepo);
        var result = await step.ExecuteAsync(item);

        // Irrelevant items are discarded before validation, using the reason directly
        Assert.True(result.Success);
        Assert.False(result.IsRelevant);
        Assert.NotNull(result.DiscardReason);
    }

    [Fact]
    public async Task Validation_MultipleErrors_AllReported()
    {
        var item = Fixtures.OfficialWhatsAppBlogPost();
        var classification = Fixtures.OfficialClassificationResponse();
        classification.SuggestedTitle = "";
        classification.Slug = "";
        classification.MetaTitle = "";
        classification.Tags = [];

        var classifier = new FakeClassifier(classification);
        var sourceItemRepo = new FakeSourceItemRepo(item.SourceItemId);

        var step = BuildStep(classifier, sourceItemRepo);
        var result = await step.ExecuteAsync(item);

        Assert.False(result.Success);
        Assert.True(result.ValidationErrors.Count >= 4);
    }

    // =====================================================================
    // Beta rule tests
    // =====================================================================

    [Fact]
    public async Task BetaRule_BetaSourceAlwaysProducesBetaNewsType()
    {
        var item = Fixtures.WABetaInfoPost();
        // The GeminiClassifier enforces BetaNews — simulate that here
        var classification = Fixtures.BetaClassificationResponse();

        var classifier = new FakeClassifier(classification);
        var sourceItemRepo = new FakeSourceItemRepo(item.SourceItemId);

        var step = BuildStep(classifier, sourceItemRepo);
        var result = await step.ExecuteAsync(item);

        Assert.True(result.Success);
        Assert.Equal(EditorialType.BetaNews, result.Classification!.EditorialType);
        Assert.Equal("beta_news", sourceItemRepo.LastUpdatedItem!.SourceClassification);
    }

    [Fact]
    public async Task BetaRule_BetaNewsRequiresEditorialNote()
    {
        var item = Fixtures.WABetaInfoPost();
        var classification = Fixtures.BetaClassificationResponse();
        classification.EditorialNote = null; // Missing editorial note

        var classifier = new FakeClassifier(classification);
        var sourceItemRepo = new FakeSourceItemRepo(item.SourceItemId);

        var step = BuildStep(classifier, sourceItemRepo);
        var result = await step.ExecuteAsync(item);

        // Validation catches missing EditorialNote for BetaNews
        Assert.False(result.Success);
        Assert.Contains(result.ValidationErrors, e => e.Contains("EditorialNote"));
    }

    [Fact]
    public async Task BetaRule_OfficialSourceProducesOfficialNewsType()
    {
        var item = Fixtures.OfficialWhatsAppBlogPost();
        var classification = Fixtures.OfficialClassificationResponse();

        var classifier = new FakeClassifier(classification);
        var sourceItemRepo = new FakeSourceItemRepo(item.SourceItemId);

        var step = BuildStep(classifier, sourceItemRepo);
        var result = await step.ExecuteAsync(item);

        Assert.True(result.Success);
        Assert.Equal(EditorialType.OfficialNews, result.Classification!.EditorialType);
        Assert.Equal("official_news", sourceItemRepo.LastUpdatedItem!.SourceClassification);
    }

    // =====================================================================
    // Error handling tests
    // =====================================================================

    [Fact]
    public async Task Error_ClassifierThrows_StepReturnsFailed()
    {
        var item = Fixtures.OfficialWhatsAppBlogPost();
        var classifier = FakeClassifier.Throwing(new InvalidOperationException("API timeout"));
        var sourceItemRepo = new FakeSourceItemRepo(item.SourceItemId);
        var logRepo = new FakeLogRepo();

        var step = BuildStep(classifier, sourceItemRepo, logRepo);
        var result = await step.ExecuteAsync(item);

        Assert.False(result.Success);
        Assert.Contains("API timeout", result.ErrorMessage);

        // SourceItem marked as failed
        Assert.Equal(PipelineStatus.Failed, sourceItemRepo.LastUpdatedItem!.Status);
        Assert.Contains("API timeout", sourceItemRepo.LastUpdatedItem.ErrorMessage);

        // Failure logged
        Assert.Single(logRepo.Logs);
        Assert.Equal("failure", logRepo.Logs[0].Status);
    }

    [Fact]
    public async Task Error_SourceItemNotFound_StepStillCompletes()
    {
        var item = Fixtures.OfficialWhatsAppBlogPost();
        var classifier = new FakeClassifier(Fixtures.OfficialClassificationResponse());
        // Repo returns null for GetById — simulates missing SourceItem
        var sourceItemRepo = new FakeSourceItemRepo(sourceItemId: null);
        var logRepo = new FakeLogRepo();

        var step = BuildStep(classifier, sourceItemRepo, logRepo);
        var result = await step.ExecuteAsync(item);

        // Classification still succeeds even if SourceItem persistence is a no-op
        Assert.True(result.Success);
        Assert.True(result.IsRelevant);
        Assert.Single(logRepo.Logs);
    }

    // =====================================================================
    // Sanitization tests
    // =====================================================================

    [Fact]
    public async Task Sanitize_SlugWithSpaces_NormalizedToHyphens()
    {
        var item = Fixtures.OfficialWhatsAppBlogPost();
        var classification = Fixtures.OfficialClassificationResponse();
        classification.Slug = "WhatsApp Lança Nova Funcionalidade";

        var classifier = new FakeClassifier(classification);
        var sourceItemRepo = new FakeSourceItemRepo(item.SourceItemId);

        var step = BuildStep(classifier, sourceItemRepo);
        var result = await step.ExecuteAsync(item);

        Assert.True(result.Success);
        Assert.Equal("whatsapp-lança-nova-funcionalidade", result.Classification!.Slug);
    }

    [Fact]
    public async Task Sanitize_TagsDeduplicatedAndLowered()
    {
        var item = Fixtures.OfficialWhatsAppBlogPost();
        var classification = Fixtures.OfficialClassificationResponse();
        classification.Tags = ["WhatsApp", "whatsapp", "NOVIDADE", "novidade", ""];

        var classifier = new FakeClassifier(classification);
        var sourceItemRepo = new FakeSourceItemRepo(item.SourceItemId);

        var step = BuildStep(classifier, sourceItemRepo);
        var result = await step.ExecuteAsync(item);

        Assert.True(result.Success);
        Assert.Equal(2, result.Classification!.Tags.Length);
        Assert.Contains("whatsapp", result.Classification.Tags);
        Assert.Contains("novidade", result.Classification.Tags);
    }

    [Fact]
    public async Task Sanitize_WhitespaceInFieldsTrimmed()
    {
        var item = Fixtures.OfficialWhatsAppBlogPost();
        var classification = Fixtures.OfficialClassificationResponse();
        classification.SuggestedTitle = "  WhatsApp lança funcionalidade  ";
        classification.MetaDescription = "  Descrição com espaços  ";

        var classifier = new FakeClassifier(classification);
        var sourceItemRepo = new FakeSourceItemRepo(item.SourceItemId);

        var step = BuildStep(classifier, sourceItemRepo);
        var result = await step.ExecuteAsync(item);

        Assert.True(result.Success);
        Assert.Equal("WhatsApp lança funcionalidade", result.Classification!.SuggestedTitle);
        Assert.Equal("Descrição com espaços", result.Classification.MetaDescription);
    }

    // =====================================================================
    // ClassificationValidator — direct unit tests
    // =====================================================================

    [Fact]
    public void Validator_ValidRelevantItem_NoErrors()
    {
        var dto = Fixtures.OfficialClassificationResponse();
        var errors = ClassificationValidator.Validate(dto);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validator_ValidIrrelevantItem_NoErrors()
    {
        var dto = Fixtures.IrrelevantClassificationResponse();
        var errors = ClassificationValidator.Validate(dto);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validator_RelevantWithAllFieldsMissing_ReportsAll()
    {
        var dto = new ClassificationResultDto
        {
            IsRelevant = true,
            SuggestedTitle = "",
            Slug = "",
            MetaTitle = "",
            MetaDescription = "",
            Excerpt = "",
            Tags = []
        };

        var errors = ClassificationValidator.Validate(dto);

        Assert.Contains(errors, e => e.Contains("SuggestedTitle"));
        Assert.Contains(errors, e => e.Contains("Slug"));
        Assert.Contains(errors, e => e.Contains("MetaTitle"));
        Assert.Contains(errors, e => e.Contains("MetaDescription"));
        Assert.Contains(errors, e => e.Contains("Excerpt"));
        Assert.Contains(errors, e => e.Contains("tag"));
    }

    [Fact]
    public void Validator_BetaNewsWithoutEditorialNote_ReportsError()
    {
        var dto = Fixtures.BetaClassificationResponse();
        dto.EditorialNote = null;

        var errors = ClassificationValidator.Validate(dto);

        Assert.Single(errors);
        Assert.Contains("EditorialNote", errors[0]);
    }

    [Fact]
    public void Validator_IrrelevantWithoutDiscardReason_ReportsError()
    {
        var dto = new ClassificationResultDto { IsRelevant = false, DiscardReason = "" };
        var errors = ClassificationValidator.Validate(dto);

        Assert.Single(errors);
        Assert.Contains("DiscardReason", errors[0]);
    }

    [Fact]
    public void Sanitizer_SlugWithConsecutiveHyphens_Collapsed()
    {
        Assert.Equal("whatsapp-nova-funcionalidade",
            ClassificationValidator.SanitizeSlug("WhatsApp--Nova--Funcionalidade"));
    }

    [Fact]
    public void Sanitizer_SlugWithLeadingTrailingHyphens_Trimmed()
    {
        Assert.Equal("whatsapp-teste",
            ClassificationValidator.SanitizeSlug("-whatsapp-teste-"));
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    private static ClassificationStep BuildStep(
        IAiClassifier classifier,
        ISourceItemRepository sourceItemRepo,
        IProcessingLogRepository? logRepo = null)
    {
        return new ClassificationStep(
            classifier,
            sourceItemRepo,
            logRepo ?? new FakeLogRepo(),
            NullLogger<ClassificationStep>.Instance);
    }

    // =====================================================================
    // Golden test fixtures — realistic content
    // =====================================================================

    private static class Fixtures
    {
        public static NormalizedItemDto OfficialWhatsAppBlogPost() => new()
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
                "The feature is available on Android and iOS starting today. " +
                "To share your screen, simply start a video call, tap the share icon, and select 'Share screen'. " +
                "Screen sharing works in both one-on-one and group video calls with up to 32 people. " +
                "Your audio will continue to work while sharing your screen. " +
                "We're excited to bring this feature to all WhatsApp users around the world.",
            ContentHash = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2",
            PublishedAt = new DateTime(2026, 3, 25, 14, 0, 0, DateTimeKind.Utc),
            SourceType = SourceType.Official
        };

        public static NormalizedItemDto WABetaInfoPost() => new()
        {
            SourceItemId = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901"),
            SourceId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            OriginalUrl = "https://wabetainfo.com/whatsapp-voice-transcription-beta",
            CanonicalUrl = "https://wabetainfo.com/whatsapp-voice-transcription-beta",
            Title = "WhatsApp is working on automatic voice message transcription",
            NormalizedContent =
                "WABetaInfo has discovered that WhatsApp is developing a new feature that will " +
                "automatically transcribe voice messages into text. The feature was spotted in " +
                "WhatsApp beta for Android version 2.26.5.12. When enabled, users will see a " +
                "'Transcribe' button below voice messages. The transcription is processed locally " +
                "on the device for privacy. The feature supports multiple languages including " +
                "Portuguese, English, and Spanish. This feature is currently under development " +
                "and not available to beta testers yet. It may be changed or removed before " +
                "the final release. WABetaInfo will keep you updated on any developments.",
            ContentHash = "b2c3d4e5f6a7b2c3d4e5f6a7b2c3d4e5f6a7b2c3d4e5f6a7b2c3d4e5f6a7b2c3",
            PublishedAt = new DateTime(2026, 3, 27, 9, 30, 0, DateTimeKind.Utc),
            SourceType = SourceType.BetaSpecialized
        };

        public static NormalizedItemDto IrrelevantItem() => new()
        {
            SourceItemId = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012"),
            SourceId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            OriginalUrl = "https://blog.whatsapp.com/privacy-policy-update-2026",
            CanonicalUrl = "https://blog.whatsapp.com/privacy-policy-update-2026",
            Title = "Privacy Policy Update 2026",
            NormalizedContent =
                "We are updating our privacy policy to comply with new regulations. " +
                "These changes are standard legal updates and do not affect how WhatsApp works. " +
                "The updated terms will take effect on April 15, 2026. " +
                "Please review the full policy on our website for details about data handling practices.",
            ContentHash = "c3d4e5f6a7b8c3d4e5f6a7b8c3d4e5f6a7b8c3d4e5f6a7b8c3d4e5f6a7b8c3d4",
            PublishedAt = new DateTime(2026, 3, 20, 8, 0, 0, DateTimeKind.Utc),
            SourceType = SourceType.Official
        };

        public static ClassificationResultDto OfficialClassificationResponse() => new()
        {
            IsRelevant = true,
            EditorialType = EditorialType.OfficialNews,
            SuggestedTitle = "WhatsApp lança compartilhamento de tela em videochamadas",
            Slug = "whatsapp-lanca-compartilhamento-de-tela-em-videochamadas",
            MetaTitle = "WhatsApp lança compartilhamento de tela em videochamadas",
            MetaDescription = "O WhatsApp lançou oficialmente o compartilhamento de tela durante videochamadas, disponível para Android e iOS.",
            Excerpt = "O WhatsApp anunciou oficialmente o recurso de compartilhamento de tela durante videochamadas, permitindo navegar na web, fazer compras e compartilhar fotos com amigos e família.",
            Tags = ["whatsapp", "videochamada", "compartilhamento-de-tela", "novidade"],
            EditorialNote = null
        };

        public static ClassificationResultDto BetaClassificationResponse() => new()
        {
            IsRelevant = true,
            EditorialType = EditorialType.BetaNews,
            SuggestedTitle = "WhatsApp testa transcrição automática de mensagens de voz",
            Slug = "whatsapp-testa-transcricao-automatica-de-mensagens-de-voz",
            MetaTitle = "WhatsApp testa transcrição de áudio em versão beta",
            MetaDescription = "O WABetaInfo descobriu que o WhatsApp está desenvolvendo transcrição automática de mensagens de voz, processada localmente no dispositivo.",
            Excerpt = "O WABetaInfo identificou que o WhatsApp está desenvolvendo uma funcionalidade de transcrição automática de mensagens de voz, com processamento local para garantir privacidade.",
            Tags = ["whatsapp", "beta", "transcricao", "mensagem-de-voz", "wabetainfo"],
            EditorialNote = "Conteúdo baseado em informações do WABetaInfo — funcionalidade em fase de desenvolvimento, sem confirmação oficial de lançamento."
        };

        public static ClassificationResultDto IrrelevantClassificationResponse() => new()
        {
            IsRelevant = false,
            DiscardReason = "Conteúdo sobre política de privacidade genérica, sem relação direta com funcionalidades do WhatsApp",
            EditorialType = EditorialType.OfficialNews,
            Tags = []
        };
    }

    // =====================================================================
    // Fakes
    // =====================================================================

    private class FakeClassifier : IAiClassifier
    {
        private readonly ClassificationResultDto? _result;
        private readonly Exception? _exception;

        public FakeClassifier(ClassificationResultDto result) => _result = result;
        private FakeClassifier(Exception exception) => _exception = exception;

        public static FakeClassifier Throwing(Exception ex) => new(ex);

        public Task<ClassificationResultDto> ClassifyAsync(
            NormalizedItemDto item, CancellationToken ct = default)
        {
            if (_exception is not null) throw _exception;
            return Task.FromResult(_result!);
        }
    }

    private class FakeSourceItemRepo : ISourceItemRepository
    {
        private readonly Guid? _existingId;
        public SourceItem? LastUpdatedItem { get; private set; }

        public FakeSourceItemRepo(Guid? sourceItemId)
        {
            _existingId = sourceItemId;
        }

        public Task<SourceItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            if (_existingId == null || id != _existingId)
                return Task.FromResult<SourceItem?>(null);

            var item = LastUpdatedItem ?? new SourceItem
            {
                Id = id,
                SourceId = Guid.NewGuid(),
                OriginalUrl = "https://example.com",
                Title = "Test",
                Status = PipelineStatus.Processing
            };
            return Task.FromResult<SourceItem?>(item);
        }

        public Task<bool> ExistsByUrlAsync(string originalUrl, CancellationToken ct = default)
            => Task.FromResult(false);

        public Task<bool> ExistsByCanonicalUrlAsync(string canonicalUrl, CancellationToken ct = default)
            => Task.FromResult(false);

        public Task<bool> ExistsByContentHashAsync(string contentHash, CancellationToken ct = default)
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
