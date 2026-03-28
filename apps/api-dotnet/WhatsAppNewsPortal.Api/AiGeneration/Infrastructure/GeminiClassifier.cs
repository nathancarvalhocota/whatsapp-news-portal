using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhatsAppNewsPortal.Api.AiGeneration.Application;
using WhatsAppNewsPortal.Api.Common;
using WhatsAppNewsPortal.Api.ContentProcessing.Application;

namespace WhatsAppNewsPortal.Api.AiGeneration.Infrastructure;

/// <summary>
/// IAiClassifier implementation using Gemini Flash-Lite via ITextGenerationProvider.
/// Classifies normalized items and produces editorial metadata.
/// </summary>
public class GeminiClassifier : IAiClassifier
{
    private readonly ITextGenerationProvider _provider;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiClassifier> _logger;

    private static readonly JsonSerializerOptions ParseOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public GeminiClassifier(
        ITextGenerationProvider provider,
        IOptions<GeminiSettings> settings,
        ILogger<GeminiClassifier> logger)
    {
        _provider = provider;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<ClassificationResultDto> ClassifyAsync(
        NormalizedItemDto item, CancellationToken ct = default)
    {
        var sourceLabel = item.SourceType == SourceType.BetaSpecialized
            ? "beta_specialized (WABetaInfo)" : "official (WhatsApp)";

        var prompt = BuildClassificationPrompt(item, sourceLabel);

        var request = new TextGenerationRequest
        {
            Model = _settings.ClassificationModel,
            Prompt = prompt,
            SystemInstruction = SystemInstruction,
            Temperature = 0.2,
            JsonMode = true
        };

        var response = await _provider.GenerateAsync(request, ct);

        if (!response.Success || string.IsNullOrWhiteSpace(response.Text))
        {
            _logger.LogError("Classification failed: {Error}", response.ErrorMessage);
            throw new InvalidOperationException($"AI classification failed: {response.ErrorMessage}");
        }

        ClassificationResultDto result;
        try
        {
            result = JsonSerializer.Deserialize<ClassificationResultDto>(response.Text, ParseOptions)
                ?? throw new JsonException("Deserialized classification result is null");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse classification response");
            throw new InvalidOperationException("Failed to parse AI classification response", ex);
        }

        EnforceEditorialRules(item, result);

        _logger.LogInformation(
            "Classification completed: isRelevant={IsRelevant}, type={EditorialType}, slug={Slug}",
            result.IsRelevant, result.EditorialType, result.Slug);

        return result;
    }

    private static void EnforceEditorialRules(NormalizedItemDto item, ClassificationResultDto result)
    {
        if (item.SourceType == SourceType.BetaSpecialized)
        {
            result.EditorialType = EditorialType.BetaNews;
            if (string.IsNullOrWhiteSpace(result.EditorialNote))
            {
                result.EditorialNote =
                    "Conteúdo baseado em informações do WABetaInfo — funcionalidade em fase de testes, sem confirmação oficial.";
            }
        }
        else
        {
            result.EditorialType = EditorialType.OfficialNews;
        }
    }

    private static string BuildClassificationPrompt(NormalizedItemDto item, string sourceLabel) =>
        $$"""
        Analise o seguinte conteúdo de uma fonte {{sourceLabel}} do ecossistema WhatsApp e classifique-o.

        TÍTULO: {{item.Title}}
        URL: {{item.CanonicalUrl}}
        CONTEÚDO:
        {{item.NormalizedContent}}

        Retorne um JSON com exatamente estes campos:
        {
          "isRelevant": true/false,
          "discardReason": "razão" ou null,
          "editorialType": "OfficialNews" ou "BetaNews",
          "suggestedTitle": "título em PT-BR",
          "slug": "slug-url-amigavel",
          "metaTitle": "título SEO em PT-BR (até 60 chars)",
          "metaDescription": "descrição SEO em PT-BR (até 160 chars)",
          "excerpt": "resumo curto em PT-BR (2-3 frases)",
          "tags": ["tag1", "tag2", ...],
          "editorialNote": "nota editorial" ou null
        }

        REGRAS:
        - Se a fonte é beta_specialized, editorialType DEVE ser "BetaNews" e editorialNote é OBRIGATÓRIA indicando conteúdo em fase de testes.
        - Se a fonte é official, editorialType DEVE ser "OfficialNews".
        - O slug deve ser derivado do título, lowercase, sem acentos, separado por hífens.
        - Tags devem incluir "whatsapp" e ser relevantes ao conteúdo.
        - Se não for relevante ao ecossistema WhatsApp, isRelevant = false com discardReason.
        - NUNCA invente informações não presentes no conteúdo.
        """;

    private const string SystemInstruction =
        """
        Você é um classificador de conteúdo especializado para um portal de notícias sobre WhatsApp em PT-BR.
        Sua tarefa é avaliar a relevância do conteúdo e gerar metadados editoriais.
        NUNCA invente informações que não estejam no conteúdo original.
        NUNCA apresente conteúdo beta como oficial ou lançamento definitivo.
        Responda APENAS com o JSON solicitado, sem texto adicional.
        """;
}
