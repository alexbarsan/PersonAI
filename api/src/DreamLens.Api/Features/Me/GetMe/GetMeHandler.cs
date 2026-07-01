using DreamLens.Api.Infrastructure.Identity;

namespace DreamLens.Api.Features.Me.GetMe;

public sealed class GetMeHandler(ICurrentUser currentUser)
{
    public GetMeResponse Handle()
    {
        return new GetMeResponse(
            currentUser.Subject,
            currentUser.Email,
            currentUser.DisplayName,
            currentUser.AuthenticationScheme);
    }
}
