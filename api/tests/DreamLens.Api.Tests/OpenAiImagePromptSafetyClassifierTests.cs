using System.Net;
using System.Text;
using System.Text.Json;
using DreamLens.Api.Infrastructure.Images;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Tests;

public sealed class OpenAiImagePromptSafetyClassifierTests
{
    [Fact]
    public async Task ClassifyAsyncUsesModerationModelAndSelectsSymbolicModeForSexualContent()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, """{"results":[{"category_scores":{"sexual":0.91,"violence":0.01}}]}""");
        using var client = new HttpClient(handler);
        var classifier = CreateClassifier(client);

        var result = await classifier.ClassifyAsync("sensitive dream", CancellationToken.None);

        Assert.Equal(DreamImagePromptMode.Symbolic, result.Mode);
        Assert.Contains("sexual", result.Categories);
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        using var payload = JsonDocument.Parse(handler.Body!);
        Assert.Equal("omni-moderation-latest", payload.RootElement.GetProperty("model").GetString());
        Assert.Equal("sensitive dream", payload.RootElement.GetProperty("input").GetString());
    }

    [Fact]
    public async Task ClassifyAsyncFallsBackToSymbolicModeWhenModerationIsUnavailable()
    {
        var handler = new RecordingHandler(HttpStatusCode.ServiceUnavailable, "{}");
        using var client = new HttpClient(handler);
        var classifier = CreateClassifier(client);

        var result = await classifier.ClassifyAsync("ordinary dream", CancellationToken.None);

        Assert.Equal(DreamImagePromptMode.Symbolic, result.Mode);
        Assert.True(result.UsedFallback);
        Assert.Contains("safety-unavailable", result.Categories);
    }

    private static OpenAiImagePromptSafetyClassifier CreateClassifier(HttpClient client) => new(
        client,
        Options.Create(new OpenAiImageOptions { BaseUrl = new Uri("https://openai.test/v1/"), ApiKey = "test-key" }),
        Options.Create(new ImagePromptSafetyOptions { Provider = "openai-moderation" }));

    private sealed class RecordingHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
    {
        public string? Body { get; private set; }
        public string? AuthorizationScheme { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        }
    }
}
