namespace WhatsAppNewsPortal.Api.Pipeline.Application;

/// <summary>
/// Orchestrates the full content pipeline: fetch → normalize → deduplicate → classify → generate draft → persist.
/// Triggered manually via endpoint. Failure in one item does not interrupt others.
/// </summary>
public interface IPipelineOrchestrator
{
    Task<PipelineRunResultDto> RunAsync(CancellationToken ct = default);
}
