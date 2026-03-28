using WhatsAppNewsPortal.Api.AiGeneration.Application;

namespace WhatsAppNewsPortal.Api.AiGeneration.Infrastructure;

/// <summary>
/// OpenAI implementation of ITextGenerationProvider.
/// Contract/placeholder for future use — NOT active in the current pipeline.
/// To activate: implement GenerateAsync, register in DI, and configure OpenAiSettings.
/// </summary>
public class OpenAiTextGenerationProvider : ITextGenerationProvider
{
    // Expected REST endpoint: POST https://api.openai.com/v1/chat/completions
    // Expected headers: Authorization: Bearer {apiKey}, Content-Type: application/json
    // Expected body: { model, messages: [{role, content}], temperature, response_format }

    public Task<TextGenerationResponse> GenerateAsync(
        TextGenerationRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException(
            "OpenAI provider is not implemented. This project uses Gemini as the AI provider. " +
            "To implement: add HttpClient calls to OpenAI Chat Completions API, " +
            "map TextGenerationRequest to OpenAI's message format, " +
            "and parse the response choices[0].message.content.");
    }
}

/// <summary>
/// Configuration for OpenAI API integration (for future use).
/// Mirror of GeminiSettings adapted for OpenAI conventions.
/// </summary>
public class OpenAiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string ClassificationModel { get; set; } = "gpt-4o-mini";
    public string GenerationModel { get; set; } = "gpt-4o";
    public int TimeoutSeconds { get; set; } = 60;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
}
