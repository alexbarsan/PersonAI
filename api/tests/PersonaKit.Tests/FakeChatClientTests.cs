using Microsoft.Extensions.AI;
using PersonaKit.Providers;

namespace PersonaKit.Tests;

public sealed class FakeChatClientTests
{
    [Fact]
    public async Task FakeChatClientReturnsConfiguredDeterministicResponse()
    {
        var client = new FakeChatClient("fixed response");

        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "ignored")],
            new ChatOptions { ModelId = "test-model" });

        Assert.Equal("fixed response", response.Text);
        Assert.Equal("fake", response.ModelId);
        var call = Assert.Single(client.Calls);
        Assert.Equal("ignored", call.Messages.Single().Text);
    }
}
