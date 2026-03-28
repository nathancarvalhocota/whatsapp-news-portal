namespace WhatsAppNewsPortal.Api.AiGeneration.Application;

/// <summary>
/// Generic abstraction for text generation via AI providers.
/// Implementations handle the HTTP transport and API specifics.
/// Current: GeminiTextGenerationProvider. Future: OpenAiTextGenerationProvider.
/// </summary>
public interface ITextGenerationProvider
{
    Task<TextGenerationResponse> GenerateAsync(TextGenerationRequest request, CancellationToken ct = default);
}
