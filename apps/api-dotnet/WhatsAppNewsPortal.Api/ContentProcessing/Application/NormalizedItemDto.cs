using WhatsAppNewsPortal.Api.Common;

namespace WhatsAppNewsPortal.Api.ContentProcessing.Application;

/// <summary>
/// Represents a SourceItem after normalization is complete.
/// Produced by IContentProcessor and used as input for the classification step.
/// </summary>
public class NormalizedItemDto
{
    public Guid SourceItemId { get; set; }
    public Guid SourceId { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string CanonicalUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string NormalizedContent { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public DateTime? PublishedAt { get; set; }
    public SourceType SourceType { get; set; }
    public bool IsDemoItem { get; set; } = false;
}
