using Microsoft.Extensions.Logging;
using WhatsAppNewsPortal.Api.AiGeneration.Application;
using WhatsAppNewsPortal.Api.Common;
using WhatsAppNewsPortal.Api.ContentProcessing.Application;
using WhatsAppNewsPortal.Api.Sources.Application;

namespace WhatsAppNewsPortal.Api.ContentProcessing.Infrastructure;

/// <summary>
/// Pipeline step that classifies a normalized item via IAiClassifier,
/// validates the structured output, persists state on the SourceItem,
/// and creates ProcessingLog entries for audit.
/// </summary>
public class ClassificationStep : IClassificationStep
{
    private readonly IAiClassifier _classifier;
    private readonly ISourceItemRepository _sourceItemRepo;
    private readonly IProcessingLogRepository _logRepo;
    private readonly ILogger<ClassificationStep> _logger;

    public ClassificationStep(
        IAiClassifier classifier,
        ISourceItemRepository sourceItemRepo,
        IProcessingLogRepository logRepo,
        ILogger<ClassificationStep> logger)
    {
        _classifier = classifier;
        _sourceItemRepo = sourceItemRepo;
        _logRepo = logRepo;
        _logger = logger;
    }

    public async Task<ClassificationStepResult> ExecuteAsync(
        NormalizedItemDto item, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting classification for SourceItem {Id}", item.SourceItemId);

        // 1. Call the AI classifier
        ClassificationResultDto classification;
        try
        {
            classification = await _classifier.ClassifyAsync(item, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Classification failed for SourceItem {Id}", item.SourceItemId);
            await PersistFailureAsync(item.SourceItemId, $"AI classification error: {ex.Message}", ct);
            return ClassificationStepResult.Failed($"AI classification error: {ex.Message}");
        }

        // 2. Handle irrelevant items → discard with log
        if (!classification.IsRelevant)
        {
            var reason = classification.DiscardReason ?? "Item deemed irrelevant by classifier";
            _logger.LogInformation("SourceItem {Id} discarded: {Reason}", item.SourceItemId, reason);
            await PersistDiscardAsync(item.SourceItemId, reason, ct);
            return ClassificationStepResult.Discarded(reason, classification);
        }

        // 3. Sanitize and validate the structured output
        ClassificationValidator.Sanitize(classification);

        var errors = ClassificationValidator.Validate(classification);
        if (errors.Count > 0)
        {
            var errorMsg = string.Join("; ", errors);
            _logger.LogWarning("Classification validation failed for {Id}: {Errors}",
                item.SourceItemId, errorMsg);
            await PersistFailureAsync(item.SourceItemId, $"Validation: {errorMsg}", ct);
            return ClassificationStepResult.Failed($"Validation errors: {errorMsg}", errors);
        }

        // 4. Persist classification on SourceItem
        var typeLabel = classification.EditorialType == EditorialType.BetaNews
            ? "beta_news" : "official_news";

        await PersistClassificationAsync(item.SourceItemId, typeLabel, ct);

        _logger.LogInformation(
            "SourceItem {Id} classified as {Type}, slug={Slug}",
            item.SourceItemId, typeLabel, classification.Slug);

        return ClassificationStepResult.Classified(classification);
    }

    private async Task PersistClassificationAsync(
        Guid sourceItemId, string typeLabel, CancellationToken ct)
    {
        var sourceItem = await _sourceItemRepo.GetByIdAsync(sourceItemId, ct);
        if (sourceItem is not null)
        {
            sourceItem.SourceClassification = typeLabel;
            sourceItem.UpdatedAt = DateTime.UtcNow;
            await _sourceItemRepo.UpdateAsync(sourceItem, ct);
        }

        await _logRepo.AddAsync(new ProcessingLog
        {
            Id = Guid.NewGuid(),
            SourceItemId = sourceItemId,
            StepName = "Classification",
            Status = "success",
            Message = $"Classified as {typeLabel}"
        }, ct);
    }

    private async Task PersistDiscardAsync(
        Guid sourceItemId, string reason, CancellationToken ct)
    {
        var sourceItem = await _sourceItemRepo.GetByIdAsync(sourceItemId, ct);
        if (sourceItem is not null)
        {
            sourceItem.Status = PipelineStatus.Failed;
            sourceItem.SourceClassification = "discarded";
            sourceItem.ErrorMessage = $"Discarded: {reason}";
            sourceItem.UpdatedAt = DateTime.UtcNow;
            await _sourceItemRepo.UpdateAsync(sourceItem, ct);
        }

        await _logRepo.AddAsync(new ProcessingLog
        {
            Id = Guid.NewGuid(),
            SourceItemId = sourceItemId,
            StepName = "Classification",
            Status = "discarded",
            Message = reason
        }, ct);
    }

    private async Task PersistFailureAsync(
        Guid sourceItemId, string error, CancellationToken ct)
    {
        var sourceItem = await _sourceItemRepo.GetByIdAsync(sourceItemId, ct);
        if (sourceItem is not null)
        {
            sourceItem.Status = PipelineStatus.Failed;
            sourceItem.ErrorMessage = error;
            sourceItem.UpdatedAt = DateTime.UtcNow;
            await _sourceItemRepo.UpdateAsync(sourceItem, ct);
        }

        await _logRepo.AddAsync(new ProcessingLog
        {
            Id = Guid.NewGuid(),
            SourceItemId = sourceItemId,
            StepName = "Classification",
            Status = "failure",
            Message = error
        }, ct);
    }
}
