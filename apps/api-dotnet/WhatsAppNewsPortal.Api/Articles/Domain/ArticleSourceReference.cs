namespace WhatsAppNewsPortal.Api.Articles.Domain;

public class ArticleSourceReference
{
    public Guid Id { get; set; }
    public Guid ArticleId { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string ReferenceType { get; set; } = string.Empty;

    public Article Article { get; set; } = null!;
}
