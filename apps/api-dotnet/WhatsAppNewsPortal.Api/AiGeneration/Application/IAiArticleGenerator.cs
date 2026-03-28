using WhatsAppNewsPortal.Api.Sources.Domain;

namespace WhatsAppNewsPortal.Api.AiGeneration.Application;

public interface IAiArticleGenerator
{
    Task<string> GenerateArticleAsync(SourceItem item, ArticleMetadata metadata, CancellationToken ct = default);
}
