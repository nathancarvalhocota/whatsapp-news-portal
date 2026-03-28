using WhatsAppNewsPortal.Api.ContentProcessing.Application;

namespace WhatsAppNewsPortal.Api.AiGeneration.Application;

/// <summary>
/// Classifies a normalized item using Gemini Flash-Lite.
/// Produces editorial metadata (slug, meta, tags, excerpt) and relevance decision.
/// Items from beta_specialized sources must always yield EditorialType = BetaNews.
/// </summary>
public interface IAiClassifier
{
    Task<ClassificationResultDto> ClassifyAsync(NormalizedItemDto item, CancellationToken ct = default);
}
