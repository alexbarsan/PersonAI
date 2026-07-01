using DreamLens.Api.Features.Me.GetMe;
using DreamLens.Api.Infrastructure.Identity;

namespace DreamLens.Api.Tests;

public sealed class GetMeHandlerTests
{
    [Fact]
    public void HandleReturnsCurrentUserFromCurrentUserAbstraction()
    {
        var currentUser = new StubCurrentUser(
            Subject: "subject-from-current-user",
            Email: "person@example.test",
            DisplayName: "Current User",
            AuthenticationScheme: "UnitTest");

        var response = new GetMeHandler(currentUser).Handle();

        Assert.Equal("subject-from-current-user", response.Subject);
        Assert.Equal("person@example.test", response.Email);
        Assert.Equal("Current User", response.DisplayName);
        Assert.Equal("UnitTest", response.AuthenticationScheme);
    }

    private sealed record StubCurrentUser(
        string Subject,
        string? Email,
        string? DisplayName,
        string AuthenticationScheme) : ICurrentUser;
}
