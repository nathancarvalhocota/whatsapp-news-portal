using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhatsAppNewsPortal.Api.AiGeneration.Application;
using WhatsAppNewsPortal.Api.Common;
using WhatsAppNewsPortal.Api.ContentProcessing.Application;

namespace WhatsAppNewsPortal.Api.AiGeneration.Infrastructure;

/// <summary>
/// IAiArticleGenerator implementation using Gemini Flash via ITextGenerationProvider.
/// Generates the final article body in PT-BR with proper HTML structure.
/// </summary>
public class GeminiArticleGenerator : IAiArticleGenerator
{
    private readonly ITextGenerationProvider _provider;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiArticleGenerator> _logger;

    private static readonly JsonSerializerOptions ParseOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public GeminiArticleGenerator(
        ITextGenerationProvider provider,
        IOptions<GeminiSettings> settings,
        ILogger<GeminiArticleGenerator> logger)
    {
        _provider = provider;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<GeneratedArticleDto> GenerateArticleAsync(
        NormalizedItemDto item,
        ClassificationResultDto classification,
        CancellationToken ct = default)
    {
        var isBeta = classification.EditorialType == EditorialType.BetaNews;
        var prompt = BuildGenerationPrompt(item, classification, isBeta);

        var request = new TextGenerationRequest
        {
            Model = _settings.GenerationModel,
            Prompt = prompt,
            SystemInstruction = SystemInstruction,
            Temperature = 0.7,
            JsonMode = true
        };

        var response = await _provider.GenerateAsync(request, ct);

        if (!response.Success || string.IsNullOrWhiteSpace(response.Text))
        {
            _logger.LogError("Article generation failed: {Error}", response.ErrorMessage);
            throw new InvalidOperationException($"AI article generation failed: {response.ErrorMessage}");
        }

        GeneratedArticleDto result;
        try
        {
            result = JsonSerializer.Deserialize<GeneratedArticleDto>(response.Text, ParseOptions)
                ?? throw new JsonException("Deserialized article result is null");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse article generation response");
            throw new InvalidOperationException("Failed to parse AI article generation response", ex);
        }

        EnforceBetaRules(isBeta, result);

        _logger.LogInformation(
            "Article generated: title={Title}, contentLength={Length}",
            result.Title, result.ContentHtml?.Length ?? 0);

        return result;
    }

    private static void EnforceBetaRules(bool isBeta, GeneratedArticleDto result)
    {
        if (isBeta && string.IsNullOrWhiteSpace(result.BetaDisclaimer))
        {
            result.BetaDisclaimer =
                "Este conteúdo é baseado em informações do WABetaInfo e refere-se a funcionalidades " +
                "ainda em fase de testes, sem confirmação oficial do WhatsApp.";
        }
        else if (!isBeta)
        {
            result.BetaDisclaimer = null;
        }
    }

    private static string BuildGenerationPrompt(
        NormalizedItemDto item, ClassificationResultDto classification, bool isBeta)
    {
        var betaContext = isBeta
            ? "\nIMPORTANTE: Este conteúdo é de fonte beta (WABetaInfo). DEVE incluir betaDisclaimer e deixar claro que NÃO é oficial."
            : "\nEste conteúdo é de fonte oficial do WhatsApp.";

        var betaRule = isBeta ? "- betaDisclaimer é OBRIGATÓRIO." : "- betaDisclaimer DEVE ser null.";

        return $$"""
            Gere um artigo completo em PT-BR para o portal de notícias sobre WhatsApp.

            TÍTULO SUGERIDO: {{classification.SuggestedTitle}}
            EXCERPT: {{classification.Excerpt}}
            NOTA EDITORIAL: {{classification.EditorialNote ?? "Nenhuma"}}

            CONTEÚDO ORIGINAL:
            {{item.NormalizedContent}}
            {{betaContext}}

            Retorne um JSON com exatamente estes campos:
            {
              "title": "título final do artigo em PT-BR",
              "excerpt": "resumo curto em PT-BR (2-3 frases)",
              "contentHtml": "corpo do artigo em HTML com H2/H3",
              "betaDisclaimer": "parágrafo de aviso beta" ou null
            }

            REGRAS DE ESCRITA:
            - O artigo deve ser ORIGINAL — NÃO tradução, cópia ou paráfrase rasa.
            - Contextualize para o público brasileiro.
            - Explique: o que mudou, quem é impactado, por que importa, impacto prático.
            - Use tags H2 e H3 para organizar seções no contentHtml.
            - Use <p> para parágrafos.
            - NÃO inclua tag H1 (será adicionada pelo sistema).
            - NÃO invente dados, datas ou funcionalidades não mencionadas no conteúdo.
            {{betaRule}}
            """;
    }

    private const string SystemInstruction =
        """
        Você é um redator editorial especializado em tecnologia e WhatsApp para o público brasileiro.
        Escreva artigos informativos, claros e originais em PT-BR.
        NUNCA copie ou traduza literalmente — reescreva com valor editorial próprio.
        NUNCA afirme disponibilidade oficial de funcionalidades que estão apenas em fase de testes.
        NUNCA invente informações que não estejam no conteúdo fornecido.
        Responda APENAS com o JSON solicitado, sem texto adicional.
        """;
}
