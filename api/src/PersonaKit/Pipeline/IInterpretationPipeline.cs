namespace PersonaKit.Pipeline;

public interface IInterpretationPipeline
{
    Task<InterpretationResponse> InterpretAsync(
        InterpretationRequest request,
        CancellationToken cancellationToken = default);
}
