namespace WhatsAppNewsPortal.Api.Demo.Application;

/// <summary>
/// Request for the demo pipeline. Provide a URL to process through the full pipeline.
/// Set Reset to true to delete previous demo data for the same URL before reprocessing.
/// </summary>
public class DemoPipelineRequest
{
    /// <summary>
    /// URL of the article to process. If empty, uses the default demo URL (if configured).
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// When true, removes any existing demo SourceItem/Article for this URL before reprocessing.
    /// Enables idempotent re-execution of the demo scenario.
    /// </summary>
    public bool Reset { get; set; } = false;
}
