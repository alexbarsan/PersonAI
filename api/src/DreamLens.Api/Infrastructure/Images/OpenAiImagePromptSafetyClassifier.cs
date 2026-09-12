using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Infrastructure.Images;

public sealed class OpenAiImagePromptSafetyClassifier(
    HttpClient httpClient,
    IOptions<OpenAiImageOptions> options,
    IOptions<ImagePromptSafetyOptions> safetyOptions) : IImagePromptSafetyClassifier
{
    public const string ProviderName = "openai-moderation";

    private static readonly string[] SymbolicCategories =
    [
        "sexual",
        "sexual/minors",
        "violence",
        "violence/graphic",
        "self-harm",
        "self-harm/intent",
        "self-harm/instructions",
        "harassment/threatening",
        "hate/threatening",
        "illicit/violent"
    ];

    private readonly HttpClient _httpClient = httpClient;
    private readonly OpenAiImageOptions _options = options.Value;
    private readonly ImagePromptSafetyOptions _safetyOptions = safetyOptions.Value;

    public async Task<ImagePromptSafetyResult> ClassifyAsync(string dreamText, CancellationToken cancellationToken)
    {
        if (!_safetyOptions.Enabled)
        {
            return new ImagePromptSafetyResult(DreamImagePromptMode.Standard, "Disabled", _safetyOptions.Model, []);
        }

        var apiKey = _options.ApiKey.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Fallback();
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_options.BaseUrl, "moderations"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(new
            {
                model = _safetyOptions.Model,
                input = dreamText
            }), Encoding.UTF8, "application/json");

            var stopwatch = Stopwatch.StartNew();
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            stopwatch.Stop();

            using var document = JsonDocument.Parse(body);
            var result = document.RootElement.GetProperty("results")[0];
            var categories = ReadCategories(result, _safetyOptions.SymbolicCategoryScoreThreshold);
            var mode = categories.Any(category => SymbolicCategories.Contains(category, StringComparer.Ordinal))
                ? DreamImagePromptMode.Symbolic
                : DreamImagePromptMode.Standard;
            return new ImagePromptSafetyResult(mode, "OpenAI", _safetyOptions.Model, categories);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Fallback();
        }
        catch (HttpRequestException)
        {
            return Fallback();
        }
        catch (JsonException)
        {
            return Fallback();
        }
        catch (KeyNotFoundException)
        {
            return Fallback();
        }
    }

    private ImagePromptSafetyResult Fallback() => new(
        DreamImagePromptMode.Symbolic,
        "OpenAI",
        _safetyOptions.Model,
        ["safety-unavailable"],
        UsedFallback: true);

    private static string[] ReadCategories(JsonElement result, decimal threshold)
    {
        if (!result.TryGetProperty("category_scores", out var scores) || scores.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        return scores.EnumerateObject()
            .Where(item => item.Value.TryGetDecimal(out var score) && score >= threshold)
            .Select(item => item.Name)
            .OrderBy(category => category, StringComparer.Ordinal)
            .ToArray();
    }
}
