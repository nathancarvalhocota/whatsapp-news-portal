namespace WhatsAppNewsPortal.Api.Demo.Application;

/// <summary>
/// Summary of a demo pipeline execution.
/// Produced by IDemoPipelineService after processing all demo fixtures.
/// </summary>
public class DemoPipelineResultDto
{
    public int ItemsDiscovered { get; set; }
    public int ItemsProcessed { get; set; }
    public int DraftsGenerated { get; set; }
    public int ArticlesPublished { get; set; }

    /// <summary>Short error descriptions for any items that failed during the run.</summary>
    public List<string> Errors { get; set; } = [];

    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    public bool HasErrors => Errors.Count > 0;
}
