using WhatsAppNewsPortal.Api.Sources.Domain;

namespace WhatsAppNewsPortal.Api.Ingestion.Application;

public interface IIngestionAdapter
{
    Task<List<SourceItem>> FetchItemsAsync(Source source, CancellationToken ct = default);
}
