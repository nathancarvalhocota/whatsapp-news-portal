using WhatsAppNewsPortal.Api.Articles.Domain;

namespace WhatsAppNewsPortal.Api.Articles.Application;

public class ArticleDetailDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Excerpt { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public string MetaTitle { get; set; } = string.Empty;
    public string MetaDescription { get; set; } = string.Empty;
    public string? SchemaJsonLd { get; set; }
    public string? Category { get; set; }
    public string[] Tags { get; set; } = [];
    public string ArticleType { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public List<SourceReferenceDto> SourceReferences { get; set; } = [];

    public static ArticleDetailDto FromArticle(Article a) => new()
    {
        Id = a.Id,
        Slug = a.Slug,
        Title = a.Title,
        Excerpt = a.Excerpt,
        ContentHtml = a.ContentHtml,
        MetaTitle = a.MetaTitle,
        MetaDescription = a.MetaDescription,
        SchemaJsonLd = a.SchemaJsonLd,
        Category = a.Category,
        Tags = a.Tags,
        ArticleType = a.ArticleType.ToString(),
        PublishedAt = a.PublishedAt ?? a.CreatedAt,
        SourceReferences = a.SourceReferences
            .Select(r => new SourceReferenceDto { SourceName = r.SourceName, SourceUrl = r.SourceUrl })
            .ToList()
    };
}
