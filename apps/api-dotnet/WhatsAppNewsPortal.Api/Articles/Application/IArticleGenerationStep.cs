using WhatsAppNewsPortal.Api.AiGeneration.Application;
using WhatsAppNewsPortal.Api.ContentProcessing.Application;

namespace WhatsAppNewsPortal.Api.Articles.Application;

/// <summary>
/// Pipeline step: generates a PT-BR article from classified content using AI,
/// validates the output, and persists it as a draft Article.
/// </summary>
public interface IArticleGenerationStep
{
    Task<ArticleGenerationStepResult> ExecuteAsync(
        NormalizedItemDto item,
        ClassificationResultDto classification,
        CancellationToken ct = default);
}
