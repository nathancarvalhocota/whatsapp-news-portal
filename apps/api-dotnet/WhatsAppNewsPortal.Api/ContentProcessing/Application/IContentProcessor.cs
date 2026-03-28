using WhatsAppNewsPortal.Api.Sources.Domain;

namespace WhatsAppNewsPortal.Api.ContentProcessing.Application;

/// <summary>
/// Normalizes a persisted SourceItem: computes canonical URL, content hash, and cleans the text.
/// Returns a NormalizedItemDto ready for the classification step.
/// </summary>
public interface IContentProcessor
{
    Task<NormalizedItemDto> NormalizeAsync(SourceItem item, CancellationToken ct = default);
}
