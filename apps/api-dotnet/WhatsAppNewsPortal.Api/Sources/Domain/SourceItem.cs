using WhatsAppNewsPortal.Api.Common;

namespace WhatsAppNewsPortal.Api.Sources.Domain;

public class SourceItem
{
    public Guid Id { get; set; }
    public Guid SourceId { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string? CanonicalUrl { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? RawContent { get; set; }
    public string? NormalizedContent { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
    public string? ContentHash { get; set; }
    public PipelineStatus Status { get; set; } = PipelineStatus.Discovered;
    public string? SourceClassification { get; set; }
    public bool IsDemoItem { get; set; } = false;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Source Source { get; set; } = null!;
}
