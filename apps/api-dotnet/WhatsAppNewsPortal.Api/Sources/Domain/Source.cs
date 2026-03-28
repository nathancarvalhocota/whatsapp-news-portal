using WhatsAppNewsPortal.Api.Common;

namespace WhatsAppNewsPortal.Api.Sources.Domain;

public class Source
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public SourceType Type { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public string? FeedUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
