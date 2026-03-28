namespace WhatsAppNewsPortal.Api.AiGeneration.Application;

/// <summary>
/// Request payload for ITextGenerationProvider.
/// Provider-agnostic: works with Gemini, OpenAI, or any future provider.
/// </summary>
public class TextGenerationRequest
{
    /// <summary>Model identifier (e.g. "gemini-2.5-flash-lite", "gpt-4o-mini").</summary>
    public required string Model { get; init; }

    /// <summary>User prompt / main content to send to the model.</summary>
    public required string Prompt { get; init; }

    /// <summary>Optional system instruction to guide model behavior.</summary>
    public string? SystemInstruction { get; init; }

    /// <summary>Sampling temperature (0.0–2.0). Lower = more deterministic.</summary>
    public double? Temperature { get; init; }

    /// <summary>When true, instructs the provider to return valid JSON.</summary>
    public bool JsonMode { get; init; }
}
