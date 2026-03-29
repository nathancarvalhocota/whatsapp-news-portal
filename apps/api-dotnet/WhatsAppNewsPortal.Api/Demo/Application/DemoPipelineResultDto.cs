namespace WhatsAppNewsPortal.Api.Demo.Application;

/// <summary>
/// Result of a demo pipeline execution.
/// </summary>
public class DemoPipelineResultDto
{
    public bool Success { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? SourceName { get; set; }
    public bool WasReset { get; set; }
    public Guid? SourceItemId { get; set; }
    public Guid? ArticleId { get; set; }
    public string? Slug { get; set; }
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Steps { get; set; } = [];
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}
