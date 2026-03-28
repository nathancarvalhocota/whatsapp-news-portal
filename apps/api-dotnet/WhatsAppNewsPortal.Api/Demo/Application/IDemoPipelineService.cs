namespace WhatsAppNewsPortal.Api.Demo.Application;

/// <summary>
/// Executes the demo pipeline using controlled fixtures from the samples directory.
/// The demo must traverse the same steps as the real pipeline (discover → normalize → classify → generate → publish).
/// All demo items are flagged with IsDemoItem = true.
/// </summary>
public interface IDemoPipelineService
{
    Task<DemoPipelineResultDto> RunDemoPipelineAsync(CancellationToken ct = default);
}
