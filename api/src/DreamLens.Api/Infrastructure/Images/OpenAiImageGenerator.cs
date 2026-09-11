using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Infrastructure.Images;

public sealed class OpenAiImageOptions
{
    public Uri BaseUrl { get; set; } = new("https://api.openai.com/v1/");

    public string ApiKey { get; set; } = string.Empty;
}

public sealed class OpenAiImageGenerator(
    HttpClient httpClient,
    IOptions<OpenAiImageOptions> options) : IImageGenerator
{
    public const string ProviderName = "openai-gpt-image";

    private readonly HttpClient _httpClient = httpClient;
    private readonly OpenAiImageOptions _options = options.Value;

    public string Provider => ProviderName;

    public async Task<ImageGenerationResult> GenerateAsync(ImageGenerationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("OpenAI:ApiKey must be configured.");
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(_options.BaseUrl, "images/generations"));
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        message.Content = new StringContent(JsonSerializer.Serialize(new
        {
            model = request.Route.Model,
            prompt = request.Prompt,
            n = 1,
            size = $"{request.Route.Width}x{request.Route.Height}",
            quality = request.Route.Quality,
            output_format = "png",
            background = "opaque",
            moderation = "auto"
        }), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OpenAI image generation failed with {(int)response.StatusCode}: {Truncate(body)}");
        }

        using var document = JsonDocument.Parse(body);
        var image = document.RootElement
            .GetProperty("data")[0]
            .GetProperty("b64_json")
            .GetString();
        if (string.IsNullOrWhiteSpace(image))
        {
            throw new InvalidOperationException("OpenAI image generation returned no image.");
        }

        return new ImageGenerationResult(
            Convert.FromBase64String(image),
            "image/png",
            "OpenAI",
            request.Route.Model);
    }

    private static string Truncate(string value) => value[..Math.Min(value.Length, 1000)];
}
