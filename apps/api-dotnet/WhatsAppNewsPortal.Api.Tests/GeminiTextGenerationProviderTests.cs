using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WhatsAppNewsPortal.Api.AiGeneration.Application;
using WhatsAppNewsPortal.Api.AiGeneration.Infrastructure;

namespace WhatsAppNewsPortal.Api.Tests;

/// <summary>
/// Testes de tratamento de falhas do GeminiTextGenerationProvider:
/// erros HTTP, timeouts e respostas inválidas da API Gemini.
/// </summary>
public class GeminiTextGenerationProviderTests
{
    private static GeminiTextGenerationProvider BuildProvider(
        HttpMessageHandler handler, string apiKey = "test-api-key")
    {
        var client = new HttpClient(handler);
        var settings = Options.Create(new GeminiSettings
        {
            ApiKey = apiKey,
            ClassificationModel = "gemini-2.5-flash-lite",
            GenerationModel = "gemini-2.5-flash",
            TimeoutSeconds = 30
        });
        return new GeminiTextGenerationProvider(
            client, settings, NullLogger<GeminiTextGenerationProvider>.Instance);
    }

    private static TextGenerationRequest BuildRequest() => new()
    {
        Model = "gemini-2.5-flash-lite",
        Prompt = "Classificar artigo do WhatsApp",
        JsonMode = true
    };

    private static string ValidGeminiResponse(string text) => JsonSerializer.Serialize(new
    {
        candidates = new[]
        {
            new
            {
                content = new { parts = new[] { new { text } } },
                finishReason = "STOP"
            }
        }
    });

    // -----------------------------------------------------------------------
    // Sucesso
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GenerateAsync_SuccessfulHttp_ReturnsTextFromResponse()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.OK, ValidGeminiResponse("resultado da IA"));
        var provider = BuildProvider(handler);

        var result = await provider.GenerateAsync(BuildRequest());

        Assert.True(result.Success);
        Assert.Equal("resultado da IA", result.Text);
        Assert.Equal("STOP", result.FinishReason);
        Assert.Null(result.ErrorMessage);
    }

    // -----------------------------------------------------------------------
    // Erros HTTP (simula erro da API Gemini)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GenerateAsync_Http500InternalServerError_ReturnsFail()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.InternalServerError, "Internal Server Error");
        var provider = BuildProvider(handler);

        var result = await provider.GenerateAsync(BuildRequest());

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("500", result.ErrorMessage);
        Assert.Null(result.Text);
    }

    [Fact]
    public async Task GenerateAsync_Http401Unauthorized_ReturnsFail()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.Unauthorized, "{\"error\": \"API key invalid\"}");
        var provider = BuildProvider(handler);

        var result = await provider.GenerateAsync(BuildRequest());

        Assert.False(result.Success);
        Assert.Contains("401", result.ErrorMessage);
        Assert.Null(result.Text);
    }

    [Fact]
    public async Task GenerateAsync_Http429TooManyRequests_ReturnsFail()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.TooManyRequests, "{\"error\": \"quota exceeded\"}");
        var provider = BuildProvider(handler);

        var result = await provider.GenerateAsync(BuildRequest());

        Assert.False(result.Success);
        Assert.Contains("429", result.ErrorMessage);
        Assert.Null(result.Text);
    }

    [Fact]
    public async Task GenerateAsync_Http503ServiceUnavailable_ReturnsFail()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.ServiceUnavailable, "Service Unavailable");
        var provider = BuildProvider(handler);

        var result = await provider.GenerateAsync(BuildRequest());

        Assert.False(result.Success);
        Assert.Contains("503", result.ErrorMessage);
    }

    // -----------------------------------------------------------------------
    // Erros de rede / HTTP exception
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GenerateAsync_HttpRequestException_ReturnsFailWithHttpErrorMessage()
    {
        var handler = new ThrowingHttpHandler(new HttpRequestException("Connection refused by server"));
        var provider = BuildProvider(handler);

        var result = await provider.GenerateAsync(BuildRequest());

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("HTTP error", result.ErrorMessage);
        Assert.Contains("Connection refused by server", result.ErrorMessage);
        Assert.Null(result.Text);
    }

    [Fact]
    public async Task GenerateAsync_HttpRequestException_DoesNotThrow()
    {
        // O provider deve absorver a exceção e retornar falha controlada
        var handler = new ThrowingHttpHandler(new HttpRequestException("DNS lookup failed"));
        var provider = BuildProvider(handler);

        // Não deve lançar exceção
        var exception = await Record.ExceptionAsync(() => provider.GenerateAsync(BuildRequest()));
        Assert.Null(exception);
    }

    // -----------------------------------------------------------------------
    // Timeout da IA
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GenerateAsync_Timeout_ReturnsFailWithTimeoutMessage()
    {
        // Simula o TaskCanceledException lançado pelo HttpClient ao expirar o timeout.
        // O CancellationToken externo NÃO está cancelado — apenas o timeout interno disparou.
        var handler = new ThrowingHttpHandler(new TaskCanceledException("Gemini API timeout simulado"));
        var provider = BuildProvider(handler);
        var externalCt = new CancellationToken(); // não cancelado

        var result = await provider.GenerateAsync(BuildRequest(), externalCt);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("timed out", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Null(result.Text);
    }

    [Fact]
    public async Task GenerateAsync_Timeout_DoesNotThrow()
    {
        var handler = new ThrowingHttpHandler(new TaskCanceledException("timeout"));
        var provider = BuildProvider(handler);

        var exception = await Record.ExceptionAsync(
            () => provider.GenerateAsync(BuildRequest(), new CancellationToken()));
        Assert.Null(exception);
    }

    // -----------------------------------------------------------------------
    // Configuração inválida
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GenerateAsync_MissingApiKey_ReturnsFailWithApiKeyMessage()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.OK, ValidGeminiResponse("text"));
        var provider = BuildProvider(handler, apiKey: "");

        var result = await provider.GenerateAsync(BuildRequest());

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("API key", result.ErrorMessage);
        Assert.Null(result.Text);
    }

    // -----------------------------------------------------------------------
    // Resposta inválida da IA
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GenerateAsync_EmptyTextInResponse_ReturnsFail()
    {
        var emptyTextResponse = JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new { parts = new[] { new { text = "" } } },
                    finishReason = "STOP"
                }
            }
        });
        var handler = new FakeHttpHandler(HttpStatusCode.OK, emptyTextResponse);
        var provider = BuildProvider(handler);

        var result = await provider.GenerateAsync(BuildRequest());

        Assert.False(result.Success);
        Assert.Contains("empty", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Null(result.Text);
    }

    [Fact]
    public async Task GenerateAsync_NoCandidates_ReturnsFail()
    {
        var noCandidatesResponse = JsonSerializer.Serialize(new { candidates = Array.Empty<object>() });
        var handler = new FakeHttpHandler(HttpStatusCode.OK, noCandidatesResponse);
        var provider = BuildProvider(handler);

        var result = await provider.GenerateAsync(BuildRequest());

        Assert.False(result.Success);
        Assert.Contains("empty", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateAsync_NullCandidates_ReturnsFail()
    {
        var nullCandidatesResponse = JsonSerializer.Serialize(new { candidates = (object?)null });
        var handler = new FakeHttpHandler(HttpStatusCode.OK, nullCandidatesResponse);
        var provider = BuildProvider(handler);

        var result = await provider.GenerateAsync(BuildRequest());

        Assert.False(result.Success);
    }

    // -----------------------------------------------------------------------
    // Fakes
    // -----------------------------------------------------------------------

    private class FakeHttpHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }

    /// <summary>Lança uma exceção ao ser chamado — simula falha de rede ou timeout.</summary>
    private class ThrowingHttpHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
            => throw exception;
    }
}
