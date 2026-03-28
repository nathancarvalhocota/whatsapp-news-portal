namespace WhatsAppNewsPortal.Api.Ingestion.Application;

/// <summary>
/// Represents a raw content item found during ingestion, before persistence.
/// Produced by IIngestionAdapter and consumed by the pipeline orchestrator.
/// </summary>
public class DiscoveredItemDto
{
    public Guid SourceId { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? RawContent { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool IsDemoItem { get; set; } = false;
}
