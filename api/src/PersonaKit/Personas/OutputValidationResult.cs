namespace PersonaKit.Personas;

public sealed record OutputValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static OutputValidationResult Valid { get; } = new(true, []);

    public static OutputValidationResult Invalid(IReadOnlyList<string> errors) => new(false, errors);
}
