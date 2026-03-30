using Microsoft.Extensions.Logging;
using WhatsAppNewsPortal.Api.AiGeneration.Application;
using WhatsAppNewsPortal.Api.Articles.Application;
using WhatsAppNewsPortal.Api.Articles.Domain;
using WhatsAppNewsPortal.Api.Common;
using WhatsAppNewsPortal.Api.ContentProcessing.Application;
using WhatsAppNewsPortal.Api.Sources.Application;
using WhatsAppNewsPortal.Api.Sources.Domain;

namespace WhatsAppNewsPortal.Api.Articles.Infrastructure;

/// <summary>
/// Pipeline step that generates a PT-BR article via IAiArticleGenerator,
/// validates the output, creates a draft Article entity, and updates the SourceItem status.
/// </summary>
public class ArticleGenerationStep : IArticleGenerationStep
{
    private readonly IAiArticleGenerator _generator;
    private readonly IArticleRepository _articleRepo;
    private readonly ISourceItemRepository _sourceItemRepo;
    private readonly IProcessingLogRepository _logRepo;
    private readonly ILogger<ArticleGenerationStep> _logger;

    public ArticleGenerationStep(
        IAiArticleGenerator generator,
        IArticleRepository articleRepo,
        ISourceItemRepository sourceItemRepo,
        IProcessingLogRepository logRepo,
        ILogger<ArticleGenerationStep> logger)
    {
        _generator = generator;
        _articleRepo = articleRepo;
        _sourceItemRepo = sourceItemRepo;
        _logRepo = logRepo;
        _logger = logger;
    }

    public async Task<ArticleGenerationStepResult> ExecuteAsync(
        NormalizedItemDto item,
        ClassificationResultDto classification,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting article generation for SourceItem {Id}", item.SourceItemId);

        // 1. Check if article already exists for this SourceItem
        if (await _articleRepo.ExistsBySourceItemIdAsync(item.SourceItemId, ct))
        {
            _logger.LogWarning("Article already exists for SourceItem {Id}", item.SourceItemId);
            await LogAsync(item.SourceItemId, "failure",
                "Article already exists for this SourceItem", ct);
            return ArticleGenerationStepResult.Failed("Article already exists for this SourceItem");
        }

        // 2. Call the AI generator
        GeneratedArticleDto generated;
        try
        {
            generated = await _generator.GenerateArticleAsync(item, classification, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Article generation failed for SourceItem {Id}", item.SourceItemId);
            await PersistFailureAsync(item.SourceItemId,
                $"AI generation error: {ex.Message}", ct);
            return ArticleGenerationStepResult.Failed($"AI generation error: {ex.Message}");
        }

        // 3. Sanitize and validate
        ArticleValidator.Sanitize(generated);

        var errors = ArticleValidator.Validate(generated, classification.EditorialType);
        if (errors.Count > 0)
        {
            var errorMsg = string.Join("; ", errors);
            _logger.LogWarning("Article validation failed for SourceItem {Id}: {Errors}",
                item.SourceItemId, errorMsg);
            await PersistFailureAsync(item.SourceItemId, $"Validation: {errorMsg}", ct);
            return ArticleGenerationStepResult.Failed(
                $"Validation errors: {errorMsg}", errors);
        }

        // 4. Build the contentHtml with beta disclaimer if applicable
        var finalHtml = BuildFinalHtml(generated);

        // 5. Ensure slug uniqueness
        var slug = await EnsureUniqueSlugAsync(classification.Slug, ct);

        // 6. Create the draft Article with source references
        var article = new Article
        {
            Id = Guid.NewGuid(),
            SourceItemId = item.SourceItemId,
            Slug = slug,
            Title = generated.Title,
            Excerpt = generated.Excerpt,
            ContentHtml = finalHtml,
            MetaTitle = classification.MetaTitle,
            MetaDescription = classification.MetaDescription,
            Tags = classification.Tags,
            Topics = generated.Topics ?? new List<string>(),
            ArticleType = classification.EditorialType,
            Status = PipelineStatus.Draft,
            Category = classification.EditorialType == EditorialType.BetaNews
                ? "beta" : "oficial"
        };

        // 7. Persist source references for editorial traceability
        var sourceItem = await _sourceItemRepo.GetByIdAsync(item.SourceItemId, ct);
        var sourceName = sourceItem?.Source?.Name ?? new Uri(item.OriginalUrl).Host;

        article.SourceReferences =
        [
            new ArticleSourceReference
            {
                Id = Guid.NewGuid(),
                ArticleId = article.Id,
                SourceName = sourceName,
                SourceUrl = item.OriginalUrl,
                ReferenceType = "primary"
            }
        ];

        await _articleRepo.AddAsync(article, ct);

        // 8. Update SourceItem status to Draft
        await UpdateSourceItemToDraftAsync(sourceItem, ct);

        // 9. Log success
        await LogAsync(item.SourceItemId, "success",
            $"Draft created: slug={slug}, articleId={article.Id}", ct);

        _logger.LogInformation(
            "Draft article created: id={ArticleId}, slug={Slug}, type={Type}",
            article.Id, slug, classification.EditorialType);

        return ArticleGenerationStepResult.Created(article.Id, slug);
    }

    private static string BuildFinalHtml(GeneratedArticleDto generated)
    {
        if (string.IsNullOrWhiteSpace(generated.BetaDisclaimer))
            return generated.ContentHtml;

        var disclaimer = $"""<aside class="beta-disclaimer"><p><strong>Atenção:</strong> {generated.BetaDisclaimer}</p></aside>""";
        return $"{disclaimer}\n{generated.ContentHtml}";
    }

    private async Task<string> EnsureUniqueSlugAsync(string baseSlug, CancellationToken ct)
    {
        var slug = baseSlug;
        var suffix = 2;

        while (await _articleRepo.GetBySlugAsync(slug, ct) is not null)
        {
            slug = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return slug;
    }

    private async Task UpdateSourceItemToDraftAsync(SourceItem? sourceItem, CancellationToken ct)
    {
        if (sourceItem is null) return;

        sourceItem.Status = PipelineStatus.Draft;
        sourceItem.UpdatedAt = DateTime.UtcNow;
        await _sourceItemRepo.UpdateAsync(sourceItem, ct);
    }

    private async Task PersistFailureAsync(Guid sourceItemId, string error, CancellationToken ct)
    {
        var sourceItem = await _sourceItemRepo.GetByIdAsync(sourceItemId, ct);
        if (sourceItem is not null)
        {
            sourceItem.Status = PipelineStatus.Failed;
            sourceItem.ErrorMessage = error;
            sourceItem.UpdatedAt = DateTime.UtcNow;
            await _sourceItemRepo.UpdateAsync(sourceItem, ct);
        }

        await LogAsync(sourceItemId, "failure", error, ct);
    }

    private async Task LogAsync(
        Guid sourceItemId, string status, string? message, CancellationToken ct)
    {
        await _logRepo.AddAsync(new ProcessingLog
        {
            Id = Guid.NewGuid(),
            SourceItemId = sourceItemId,
            StepName = "ArticleGeneration",
            Status = status,
            Message = message
        }, ct);
    }
}
