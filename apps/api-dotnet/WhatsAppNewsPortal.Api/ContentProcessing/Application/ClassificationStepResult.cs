using WhatsAppNewsPortal.Api.AiGeneration.Application;

namespace WhatsAppNewsPortal.Api.ContentProcessing.Application;

/// <summary>
/// Result of the classification pipeline step.
/// Success means the AI responded and the result was validated (even if discarded).
/// </summary>
public class ClassificationStepResult
{
    public bool Success { get; init; }
    public bool IsRelevant { get; init; }
    public string? DiscardReason { get; init; }
    public ClassificationResultDto? Classification { get; init; }
    public List<string> ValidationErrors { get; init; } = [];
    public string? ErrorMessage { get; init; }

    public static ClassificationStepResult Discarded(string reason, ClassificationResultDto classification) =>
        new() { Success = true, IsRelevant = false, DiscardReason = reason, Classification = classification };

    public static ClassificationStepResult Classified(ClassificationResultDto classification) =>
        new() { Success = true, IsRelevant = true, Classification = classification };

    public static ClassificationStepResult Failed(string error, List<string>? validationErrors = null) =>
        new() { Success = false, ErrorMessage = error, ValidationErrors = validationErrors ?? [] };
}
