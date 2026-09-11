using System.Net;
using System.Text;
using System.Text.Json;
using DreamLens.Api.Infrastructure.Images;
using DreamLens.Api.Infrastructure.Monetization;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Tests;

public sealed class OpenAiImageGeneratorTests
{
    [Fact]
    public async Task GenerateAsyncUsesPersistedRouteAndReturnsPng()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, """{"data":[{"b64_json":"aGVsbG8="}]}""");
        using var client = new HttpClient(handler);
        var generator = new OpenAiImageGenerator(client, Options.Create(new OpenAiImageOptions
        {
            BaseUrl = new Uri("https://openai.test/v1/"),
            ApiKey = "test-key"
        }));
        var route = new ImageGenerationRoute(EntitlementTier.Free, true, OpenAiImageGenerator.ProviderName,
            "gpt-image-1-mini", 1024, 1024, 0.005m, "low");

        var result = await generator.GenerateAsync(new ImageGenerationRequest("moonlit water", "SOFT_DIGITAL_PAINTING", route), CancellationToken.None);

        Assert.Equal("OpenAI", result.Provider);
        Assert.Equal("gpt-image-1-mini", result.Model);
        Assert.Equal("image/png", result.ContentType);
        Assert.Equal("hello", Encoding.UTF8.GetString(result.Content));
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal("test-key", handler.AuthorizationParameter);
        using var payload = JsonDocument.Parse(handler.Body!);
        Assert.Equal("gpt-image-1-mini", payload.RootElement.GetProperty("model").GetString());
        Assert.Equal("moonlit water", payload.RootElement.GetProperty("prompt").GetString());
        Assert.Equal("1024x1024", payload.RootElement.GetProperty("size").GetString());
        Assert.Equal("low", payload.RootElement.GetProperty("quality").GetString());
        Assert.Equal("png", payload.RootElement.GetProperty("output_format").GetString());
    }

    [Fact]
    public async Task GenerateAsyncDoesNotCallProviderWithoutApiKey()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "{}");
        using var client = new HttpClient(handler);
        var generator = new OpenAiImageGenerator(client, Options.Create(new OpenAiImageOptions()));
        var route = new ImageGenerationRoute(EntitlementTier.Premium, true, OpenAiImageGenerator.ProviderName,
            "gpt-image-1-mini", 1024, 1024, 0.011m, "medium");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => generator.GenerateAsync(
            new ImageGenerationRequest("moonlit water", "SOFT_DIGITAL_PAINTING", route), CancellationToken.None));

        Assert.Equal("OpenAI:ApiKey must be configured.", exception.Message);
        Assert.False(handler.WasCalled);
    }

    private sealed class RecordingHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
    {
        public string? Body { get; private set; }
        public string? AuthorizationScheme { get; private set; }
        public string? AuthorizationParameter { get; private set; }
        public bool WasCalled { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            WasCalled = true;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        }
    }
}
