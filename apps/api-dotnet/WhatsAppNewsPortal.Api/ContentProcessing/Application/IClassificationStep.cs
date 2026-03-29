namespace WhatsAppNewsPortal.Api.ContentProcessing.Application;

/// <summary>
/// Pipeline step: classifies a normalized item using AI and produces editorial metadata.
/// Updates SourceItem state and creates ProcessingLog entries.
/// </summary>
public interface IClassificationStep
{
    Task<ClassificationStepResult> ExecuteAsync(NormalizedItemDto item, CancellationToken ct = default);
}
