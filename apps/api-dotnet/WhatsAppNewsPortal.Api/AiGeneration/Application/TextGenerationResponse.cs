namespace WhatsAppNewsPortal.Api.AiGeneration.Application;

/// <summary>
/// Response from ITextGenerationProvider.
/// Check Success before accessing Text.
/// </summary>
public class TextGenerationResponse
{
    public bool Success { get; init; }
    public string? Text { get; init; }
    public string? ErrorMessage { get; init; }
    public string? FinishReason { get; init; }
}
