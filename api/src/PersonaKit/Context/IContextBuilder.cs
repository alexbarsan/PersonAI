namespace PersonaKit.Context;

public interface IContextBuilder
{
    Task<string> BuildAsync(ContextBuildRequest request, CancellationToken cancellationToken = default);
}
