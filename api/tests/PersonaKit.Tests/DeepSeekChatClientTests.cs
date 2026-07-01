using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using PersonaKit.Providers.DeepSeek;

namespace PersonaKit.Tests;

public sealed class DeepSeekChatClientTests
{
    [Fact]
    public async Task SendsOpenAiCompatibleRequestShape()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                id = "chatcmpl-test",
                model = "deepseek-chat",
                choices = new[]
                {
                    new
                    {
                        message = new { role = "assistant", content = "{\"ok\":true}" },
                        finish_reason = "stop"
                    }
                },
                usage = new { prompt_tokens = 11, completion_tokens = 7, total_tokens = 18 }
            })
        });
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://stub.local")
        };
        var client = new DeepSeekChatClient(
            httpClient,
            Options.Create(new DeepSeekOptions
            {
                ApiKey = "secret-key",
                Model = "deepseek-chat",
                BaseUrl = new Uri("https://stub.local")
            }));

        var response = await client.GetResponseAsync(
            [
                new ChatMessage(ChatRole.System, "system prompt"),
                new ChatMessage(ChatRole.User, "user prompt")
            ],
            new ChatOptions { Temperature = 0.2f, MaxOutputTokens = 256 });

        Assert.Equal(HttpMethod.Post, handler.Requests.Single().Method);
        Assert.Equal("/chat/completions", handler.Requests.Single().RequestUri?.AbsolutePath);
        Assert.Equal("Bearer", handler.Requests.Single().Headers.Authorization?.Scheme);
        Assert.Equal("secret-key", handler.Requests.Single().Headers.Authorization?.Parameter);
        Assert.NotNull(handler.Body);

        using var document = JsonDocument.Parse(handler.Body);
        var root = document.RootElement;
        Assert.Equal("deepseek-chat", root.GetProperty("model").GetString());
        Assert.Equal(0.2, root.GetProperty("temperature").GetDouble(), precision: 3);
        Assert.Equal(256, root.GetProperty("max_tokens").GetInt32());
        Assert.Equal("system", root.GetProperty("messages")[0].GetProperty("role").GetString());
        Assert.Equal("system prompt", root.GetProperty("messages")[0].GetProperty("content").GetString());
        Assert.Equal("user", root.GetProperty("messages")[1].GetProperty("role").GetString());
        Assert.Equal("user prompt", root.GetProperty("messages")[1].GetProperty("content").GetString());

        Assert.Equal("{\"ok\":true}", response.Text);
        Assert.Equal("deepseek-chat", response.ModelId);
        Assert.Equal(11, response.Usage?.InputTokenCount);
        Assert.Equal(7, response.Usage?.OutputTokenCount);
        Assert.Equal(18, response.Usage?.TotalTokenCount);
    }
}
