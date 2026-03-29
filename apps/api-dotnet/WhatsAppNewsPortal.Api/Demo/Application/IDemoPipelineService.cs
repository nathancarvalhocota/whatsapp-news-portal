namespace WhatsAppNewsPortal.Api.Demo.Application;

/// <summary>
/// Executes the demo pipeline for a specific URL, traversing the same steps as the real pipeline
/// (fetch → normalize → classify → generate draft). All demo items are flagged with IsDemoItem = true.
/// Supports reset to allow reproducible re-execution.
/// </summary>
public interface IDemoPipelineService
{
    Task<DemoPipelineResultDto> RunDemoAsync(DemoPipelineRequest request, CancellationToken ct = default);
}
