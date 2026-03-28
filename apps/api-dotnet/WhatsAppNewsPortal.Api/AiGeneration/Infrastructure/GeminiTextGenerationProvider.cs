using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhatsAppNewsPortal.Api.AiGeneration.Application;

namespace WhatsAppNewsPortal.Api.AiGeneration.Infrastructure;

/// <summary>
/// Gemini REST API implementation of ITextGenerationProvider.
/// Calls generativelanguage.googleapis.com/v1beta/models/{model}:generateContent.
/// </summary>
public class GeminiTextGenerationProvider : ITextGenerationProvider
{
    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiTextGenerationProvider> _logger;

    private static readonly JsonSerializerOptions SerializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GeminiTextGenerationProvider(
        HttpClient httpClient,
        IOptions<GeminiSettings> settings,
        ILogger<GeminiTextGenerationProvider> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<TextGenerationResponse> GenerateAsync(
        TextGenerationRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _logger.LogError("Gemini API key is not configured");
            return Fail("Gemini API key is not configured. Set GEMINI_API_KEY environment variable.");
        }

        var url = $"{_settings.BaseUrl}/models/{request.Model}:generateContent";
        var body = BuildRequestBody(request);

        try
        {
            _logger.LogInformation("Calling Gemini API model={Model}", request.Model);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Headers.Add("x-goog-api-key", _settings.ApiKey);
            httpRequest.Content = JsonContent.Create(body, options: SerializeOptions);

            using var httpResponse = await _httpClient.SendAsync(httpRequest, ct);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorBody = await httpResponse.Content.ReadAsStringAsync(ct);
                _logger.LogError("Gemini API returned {StatusCode}: {Error}",
                    (int)httpResponse.StatusCode, TruncateForLog(errorBody));
                return Fail($"Gemini API error: {(int)httpResponse.StatusCode} {httpResponse.StatusCode}");
            }

            var geminiResponse = await httpResponse.Content
                .ReadFromJsonAsync<GeminiApiResponse>(DeserializeOptions, ct);

            var text = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
            var finishReason = geminiResponse?.Candidates?.FirstOrDefault()?.FinishReason;

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Gemini returned empty response, finishReason={FinishReason}", finishReason);
                return Fail($"Gemini returned empty response (finishReason: {finishReason})");
            }

            _logger.LogInformation(
                "Gemini response received, length={Length}, finishReason={FinishReason}",
                text.Length, finishReason);

            return new TextGenerationResponse
            {
                Success = true,
                Text = text,
                FinishReason = finishReason
            };
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Gemini API request timed out for model={Model}", request.Model);
            return Fail("Gemini API request timed out");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Gemini API for model={Model}", request.Model);
            return Fail($"HTTP error: {ex.Message}");
        }
    }

    private static Dictionary<string, object> BuildRequestBody(TextGenerationRequest request)
    {
        var body = new Dictionary<string, object>
        {
            ["contents"] = new[] { new { parts = new[] { new { text = request.Prompt } } } }
        };

        if (!string.IsNullOrWhiteSpace(request.SystemInstruction))
        {
            body["system_instruction"] = new
            {
                parts = new[] { new { text = request.SystemInstruction } }
            };
        }

        var genConfig = new Dictionary<string, object>();

        if (request.Temperature.HasValue)
            genConfig["temperature"] = request.Temperature.Value;

        if (request.JsonMode)
            genConfig["responseMimeType"] = "application/json";

        if (genConfig.Count > 0)
            body["generationConfig"] = genConfig;

        return body;
    }

    private static TextGenerationResponse Fail(string message) =>
        new() { Success = false, ErrorMessage = message };

    private static string TruncateForLog(string value, int maxLength = 500) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...(truncated)";

    // --- Gemini REST API response models ---

    private record GeminiApiResponse(
        [property: JsonPropertyName("candidates")] GeminiCandidate[]? Candidates);

    private record GeminiCandidate(
        [property: JsonPropertyName("content")] GeminiContent? Content,
        [property: JsonPropertyName("finishReason")] string? FinishReason);

    private record GeminiContent(
        [property: JsonPropertyName("parts")] GeminiPart[]? Parts);

    private record GeminiPart(
        [property: JsonPropertyName("text")] string? Text);
}
