namespace WhatsAppNewsPortal.Api.Common;

public class ProcessingLog
{
    public Guid Id { get; set; }
    public Guid? SourceItemId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
