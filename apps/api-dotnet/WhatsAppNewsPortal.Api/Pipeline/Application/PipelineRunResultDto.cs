namespace WhatsAppNewsPortal.Api.Pipeline.Application;

public class PipelineRunResultDto
{
    public int SourcesProcessed { get; set; }
    public int ItemsDiscovered { get; set; }
    public int ItemsNormalized { get; set; }
    public int ItemsDeduplicated { get; set; }
    public int ItemsClassified { get; set; }
    public int DraftsGenerated { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<PipelineItemSummary> Items { get; set; } = [];
    public DateTime StartedAt { get; set; }
    public DateTime FinishedAt { get; set; }
    public bool HasErrors => Errors.Count > 0;
}

public class PipelineItemSummary
{
    public string SourceName { get; set; } = "";
    public string Title { get; set; } = "";
    public string Status { get; set; } = "";
    public Guid? ArticleId { get; set; }
    public string? Slug { get; set; }
    public string? Error { get; set; }
}
