using WhatsAppNewsPortal.Api.Articles.Domain;
using WhatsAppNewsPortal.Api.Sources.Domain;

namespace WhatsAppNewsPortal.Api.AiGeneration.Application;

public interface IAiClassifier
{
    Task<ArticleMetadata> ClassifyAsync(SourceItem item, CancellationToken ct = default);
}

public class ArticleMetadata
{
    public string SuggestedTitle { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string MetaDescription { get; set; } = string.Empty;
    public string[] Tags { get; set; } = [];
    public Common.EditorialType EditorialType { get; set; }
}
