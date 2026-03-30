using WhatsAppNewsPortal.Api.Common;

namespace WhatsAppNewsPortal.Api.Articles.Domain;

public class Article
{
    public Guid Id { get; set; }
    public Guid SourceItemId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Excerpt { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public string MetaTitle { get; set; } = string.Empty;
    public string MetaDescription { get; set; } = string.Empty;
    public string? SchemaJsonLd { get; set; }
    public string? Category { get; set; }
    public string[] Tags { get; set; } = [];
    public List<string> Topics { get; set; } = new();
    public EditorialType ArticleType { get; set; }
    public PipelineStatus Status { get; set; } = PipelineStatus.Draft;
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Sources.Domain.SourceItem SourceItem { get; set; } = null!;
    public ICollection<ArticleSourceReference> SourceReferences { get; set; } = [];
}
