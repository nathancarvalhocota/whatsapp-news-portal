using WhatsAppNewsPortal.Api.Sources.Domain;

namespace WhatsAppNewsPortal.Api.Ingestion.Application;

/// <summary>
/// Fetches raw content items from a monitored source.
/// Implementations must not persist items — they return DiscoveredItemDtos
/// that the pipeline orchestrator is responsible for persisting.
/// </summary>
public interface IIngestionAdapter
{
    Task<List<DiscoveredItemDto>> FetchItemsAsync(Source source, CancellationToken ct = default);
}
