using WhatsAppNewsPortal.Api.Sources.Domain;

namespace WhatsAppNewsPortal.Api.ContentProcessing.Application;

public interface IContentProcessor
{
    Task<SourceItem> NormalizeAsync(SourceItem item, CancellationToken ct = default);
}
