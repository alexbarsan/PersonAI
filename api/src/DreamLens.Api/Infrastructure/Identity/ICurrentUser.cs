namespace DreamLens.Api.Infrastructure.Identity;

public interface ICurrentUser
{
    string Subject { get; }

    string? Email { get; }

    string? DisplayName { get; }

    string AuthenticationScheme { get; }
}
