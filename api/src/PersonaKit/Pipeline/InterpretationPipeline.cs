using Microsoft.Extensions.AI;
using PersonaKit.Context;
using PersonaKit.Personas;

namespace PersonaKit.Pipeline;

public sealed class InterpretationPipeline(
    IPersonaRegistry personaRegistry,
    IContextBuilder contextBuilder,
    IPromptRenderer promptRenderer,
    IChatClient chatClient,
    IOutputValidator outputValidator,
    IResultSectionMapper resultSectionMapper,
    IInterpretationStore interpretationStore,
    IAiRunStore aiRunStore,
    IModerationPrecheck moderationPrecheck) : IInterpretationPipeline
{
    private const string FriendlyValidationFailure = "The interpretation service could not produce a valid result. Please try again.";
    private const string FriendlyProviderFailure = "The interpretation service is temporarily unavailable. Please try again.";
    private const string RepairPrompt = """
        Your previous response was invalid.

        Return only valid JSON matching the provided schema. Do not include markdown, comments, code fences, explanations, or extra fields. Preserve the original meaning as much as possible while correcting structure, types, required fields, and enum values.
        """;

    public async Task<InterpretationResponse> InterpretAsync(
        InterpretationRequest request,
        CancellationToken cancellationToken = default)
    {
        var interpretationId = $"interp_{Guid.NewGuid():N}";
        var runId = $"run_{Guid.NewGuid():N}";
        var persona = await personaRegistry.GetAsync(request.PersonaId, cancellationToken);

        var moderation = await moderationPrecheck.CheckAsync(request, cancellationToken);
        if (!moderation.IsAllowed)
        {
            await RecordRunAsync(runId, persona.Id, AiRunStatus.Failed, 0, AiRunFailureKind.Moderation, null, cancellationToken);
            return new InterpretationResponse(interpretationId, InterpretationStatus.Failed, null, moderation.FailureMessage);
        }

        var contextJson = await contextBuilder.BuildAsync(request.ContextRequest, cancellationToken);
        var schemaJson = await File.ReadAllTextAsync(persona.OutputSchemaPath, cancellationToken);
        var prompt = await promptRenderer.RenderAsync(
            persona,
            new Dictionary<string, object?>
            {
                ["context_json"] = contextJson,
                ["persona_id"] = persona.Id,
                ["persona_version"] = persona.Version,
                ["locale"] = request.ContextRequest.Locale,
                ["output_schema_json"] = schemaJson
            },
            cancellationToken);

        var attempts = 0;
        ChatResponse response;
        try
        {
            attempts++;
            response = await chatClient.GetResponseAsync([new ChatMessage(ChatRole.System, prompt)], cancellationToken: cancellationToken);
        }
        catch (Exception)
        {
            await RecordRunAsync(runId, persona.Id, AiRunStatus.Failed, attempts, AiRunFailureKind.Provider, null, cancellationToken);
            return new InterpretationResponse(interpretationId, InterpretationStatus.Failed, null, FriendlyProviderFailure);
        }

        var outputJson = response.Text;
        var validation = await outputValidator.ValidateAsync(persona, outputJson, cancellationToken);
        if (!validation.IsValid)
        {
            try
            {
                attempts++;
                response = await chatClient.GetResponseAsync(
                    [
                        new ChatMessage(ChatRole.System, prompt),
                        new ChatMessage(ChatRole.Assistant, outputJson),
                        new ChatMessage(ChatRole.User, BuildRepairPrompt(outputJson, schemaJson))
                    ],
                    cancellationToken: cancellationToken);
                outputJson = response.Text;
                validation = await outputValidator.ValidateAsync(persona, outputJson, cancellationToken);
            }
            catch (Exception)
            {
                await RecordRunAsync(runId, persona.Id, AiRunStatus.Failed, attempts, AiRunFailureKind.Provider, null, cancellationToken);
                return new InterpretationResponse(interpretationId, InterpretationStatus.Failed, null, FriendlyProviderFailure);
            }
        }

        if (!validation.IsValid)
        {
            await RecordRunAsync(runId, persona.Id, AiRunStatus.Failed, attempts, AiRunFailureKind.Validation, response, cancellationToken);
            return new InterpretationResponse(interpretationId, InterpretationStatus.Failed, null, FriendlyValidationFailure);
        }

        var result = await resultSectionMapper.MapAsync(persona, outputJson, cancellationToken);
        await interpretationStore.SaveAsync(
            new InterpretationRecord(interpretationId, persona.Id, result.Summary, outputJson, DateTimeOffset.UtcNow),
            cancellationToken);
        await RecordRunAsync(runId, persona.Id, AiRunStatus.Succeeded, attempts, null, response, cancellationToken);

        return new InterpretationResponse(interpretationId, InterpretationStatus.Completed, result, null);
    }

    private static string BuildRepairPrompt(string invalidOutput, string schemaJson)
    {
        return $"""
            {RepairPrompt}

            Invalid response:
            {invalidOutput}

            Required schema:
            {schemaJson}
            """;
    }

    private async Task RecordRunAsync(
        string runId,
        string personaId,
        AiRunStatus status,
        int attempts,
        AiRunFailureKind? failureKind,
        ChatResponse? response,
        CancellationToken cancellationToken)
    {
        await aiRunStore.RecordAsync(
            new AiRunRecord(
                runId,
                personaId,
                status,
                attempts,
                failureKind,
                ToInt(response?.Usage?.InputTokenCount),
                ToInt(response?.Usage?.OutputTokenCount),
                DateTimeOffset.UtcNow),
            cancellationToken);
    }

    private static int? ToInt(long? value)
    {
        return value is null ? null : checked((int)value.Value);
    }
}
