using WhatsAppNewsPortal.Api.ContentProcessing.Application;

namespace WhatsAppNewsPortal.Api.AiGeneration.Application;

/// <summary>
/// Generates the final article body in PT-BR using Gemini Flash.
/// Requires a NormalizedItemDto for source content and a ClassificationResultDto for editorial metadata.
/// </summary>
public interface IAiArticleGenerator
{
    Task<GeneratedArticleDto> GenerateArticleAsync(
        NormalizedItemDto item,
        ClassificationResultDto classification,
        CancellationToken ct = default);
}
