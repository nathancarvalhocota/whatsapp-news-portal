namespace WhatsAppNewsPortal.Api.AiGeneration.Infrastructure;

/// <summary>
/// Configuration for Gemini API integration.
/// Populated from environment variables in Program.cs.
/// </summary>
public class GeminiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string ClassificationModel { get; set; } = "gemini-2.5-flash-lite";
    public string GenerationModel { get; set; } = "gemini-2.5-flash";
    public int TimeoutSeconds { get; set; } = 60;
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
}
