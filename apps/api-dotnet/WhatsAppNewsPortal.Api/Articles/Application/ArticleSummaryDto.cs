using WhatsAppNewsPortal.Api.Articles.Domain;

namespace WhatsAppNewsPortal.Api.Articles.Application;

public class ArticleSummaryDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Excerpt { get; set; } = string.Empty;
    public string MetaDescription { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string[] Tags { get; set; } = [];
    public List<string> Topics { get; set; } = new();
    public string ArticleType { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }

    public static ArticleSummaryDto FromArticle(Article a) => new()
    {
        Id = a.Id,
        Slug = a.Slug,
        Title = a.Title,
        Excerpt = a.Excerpt,
        MetaDescription = a.MetaDescription,
        Category = a.Category,
        Tags = a.Tags,
        Topics = a.Topics,
        ArticleType = a.ArticleType.ToString(),
        PublishedAt = a.PublishedAt ?? a.CreatedAt
    };
}
