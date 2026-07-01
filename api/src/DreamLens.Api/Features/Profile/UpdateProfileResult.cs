namespace DreamLens.Api.Features.Profile;

public sealed class UpdateProfileResult
{
    private UpdateProfileResult(ProfileResponse? profile, Dictionary<string, string[]> errors)
    {
        Profile = profile;
        Errors = errors;
    }

    public bool IsValid => Errors.Count == 0;

    public ProfileResponse? Profile { get; }

    public Dictionary<string, string[]> Errors { get; }

    public static UpdateProfileResult Valid(ProfileResponse profile)
    {
        return new UpdateProfileResult(profile, []);
    }

    public static UpdateProfileResult Invalid(Dictionary<string, string[]> errors)
    {
        return new UpdateProfileResult(null, errors);
    }
}
