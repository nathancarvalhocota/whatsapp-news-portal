namespace WhatsAppNewsPortal.Api.Articles.Application;

/// <summary>
/// Result of publishing a draft article.
/// Produced by IArticlePublisher after transitioning status to Published.
/// </summary>
public class PublishArticleResultDto
{
    public Guid ArticleId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
}
