using System.Diagnostics;
using System.Text.Json;
using DreamLens.Api.Infrastructure.Embeddings;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Monetization;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using PersonaKit.Personas;
using PersonaKit.Providers.DeepSeek;
using PersonaKit.Providers.Usage;

namespace DreamLens.Api.Features.Dreams;

public sealed class AskDreamsHandler(
    DreamLensDbContext dbContext,
    ICurrentUser currentUser,
    IEntitlementService entitlementService,
    IEmbeddingProvider embeddingProvider,
    SemanticMemoryService semanticMemory,
    IPersonaRegistry personaRegistry,
    IPromptRenderer promptRenderer,
    IOutputValidator outputValidator,
    IChatClient chatClient,
    IOptions<AskDreamsOptions> askOptions,
    IOptions<EmbeddingOptions> embeddingOptions,
    IOptions<DeepSeekOptions> deepSeekOptions,
    IOptions<UsageCostOptions> usageCostOptions)
{
    private const string PersonaId = "dream-history-guide";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AskDreamsResult> HandleAsync(AskDreamsRequest request, CancellationToken cancellationToken)
    {
        var question = request.Question?.Trim();
        if (string.IsNullOrWhiteSpace(question) || question.Length < 5)
        {
            return AskDreamsResult.Failure(StatusCodes.Status400BadRequest, "question", "Question must be at least 5 characters.");
        }

        if (question.Length > askOptions.Value.MaxQuestionLength)
        {
            return AskDreamsResult.Failure(StatusCodes.Status400BadRequest, "question", $"Question must be {askOptions.Value.MaxQuestionLength} characters or fewer.");
        }

        var profile = await dbContext.UserProfiles.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.UserSubject == currentUser.Subject, cancellationToken);
        if (profile is null)
        {
            return AskDreamsResult.Failure(StatusCodes.Status409Conflict, "profile", "Complete your profile before asking about dream history.");
        }

        if (!profile.ConsentAiProcessing || !profile.ConsentHistoryUse)
        {
            return AskDreamsResult.Failure(StatusCodes.Status409Conflict, "consent", "AI processing and dream history consent are required.");
        }

        if (!embeddingOptions.Value.Enabled)
        {
            return MemoryUnavailable();
        }

        var entitlement = entitlementService.GetEntitlement(currentUser.Subject);
        var dailyLimit = entitlement.Tier == EntitlementTier.Premium
            ? askOptions.Value.PremiumDailyLimit
            : askOptions.Value.FreeDailyLimit;
        var today = DateTimeOffset.UtcNow.Date;
        var completedToday = await dbContext.AiCostLedger.AsNoTracking().CountAsync(
            row => row.UserSubject == currentUser.Subject
                && row.OperationType == "dream.ask"
                && row.Status == "completed"
                && row.CreatedAt >= today,
            cancellationToken);
        if (completedToday >= dailyLimit)
        {
            return AskDreamsResult.Failure(StatusCodes.Status429TooManyRequests, "quota", "You have reached today's dream-history question limit.");
        }

        EmbeddingResult queryEmbedding;
        var embeddingStarted = Stopwatch.GetTimestamp();
        try
        {
            queryEmbedding = await embeddingProvider.CreateAsync(question, cancellationToken);
            dbContext.AiCostLedger.Add(CreateEmbeddingLedger(queryEmbedding, "completed", null, Stopwatch.GetElapsedTime(embeddingStarted)));
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            dbContext.AiCostLedger.Add(CreateFailedEmbeddingLedger(exception, Stopwatch.GetElapsedTime(embeddingStarted)));
            await dbContext.SaveChangesAsync(cancellationToken);
            return MemoryUnavailable();
        }

        var matches = await semanticMemory.FindSimilarAsync(
            currentUser.Subject,
            queryEmbedding.Values,
            askOptions.Value.RetrievalLimit,
            cancellationToken);
        var dreamIds = matches.Select(match => match.DreamId).Distinct().Take(askOptions.Value.RetrievalLimit).ToArray();
        var dreams = await dbContext.Dreams.AsNoTracking()
            .Where(dream => dream.UserSubject == currentUser.Subject && dream.Status == "completed" && dreamIds.Contains(dream.Id))
            .ToListAsync(cancellationToken);
        var sources = dreamIds.Select(id => dreams.SingleOrDefault(dream => dream.Id == id))
            .Where(dream => dream is not null && !string.IsNullOrWhiteSpace(DreamMapper.ReadSummary(dream)))
            .Cast<DreamRecord>()
            .ToArray();
        if (sources.Length == 0)
        {
            return MemoryUnavailable();
        }

        var persona = await personaRegistry.GetAsync(PersonaId, cancellationToken);
        var schemaJson = await File.ReadAllTextAsync(persona.OutputSchemaPath, cancellationToken);
        var memoryJson = JsonSerializer.Serialize(sources.Select(dream => new
        {
            id = dream.Id,
            summary = DreamMapper.ReadSummary(dream),
            dream.OccurredAt,
            dream.CreatedAt
        }), JsonOptions);
        var prompt = await promptRenderer.RenderAsync(persona, new Dictionary<string, object?>
        {
            ["question_json"] = JsonSerializer.Serialize(question, JsonOptions),
            ["memory_json"] = memoryJson,
            ["output_schema_json"] = schemaJson
        }, cancellationToken);

        var askStarted = Stopwatch.GetTimestamp();
        var attempts = 0;
        var inputTokens = 0;
        var outputTokens = 0;
        ChatResponse? chatResponse = null;
        AskModelOutput? modelOutput = null;
        string? failureKind = null;
        try
        {
            for (var attempt = 1; attempt <= 2; attempt++)
            {
                attempts = attempt;
                chatResponse = await chatClient.GetResponseAsync(
                    attempt == 1
                        ? [new ChatMessage(ChatRole.System, prompt)]
                        : [
                            new ChatMessage(ChatRole.System, prompt),
                            new ChatMessage(ChatRole.Assistant, chatResponse?.Text ?? ""),
                            new ChatMessage(ChatRole.User, "Return only corrected JSON matching the schema. Every referencedDreamId must be one of the provided memory ids.")
                        ],
                    cancellationToken: cancellationToken);
                inputTokens += ToInt(chatResponse.Usage?.InputTokenCount);
                outputTokens += ToInt(chatResponse.Usage?.OutputTokenCount);
                var validation = await outputValidator.ValidateAsync(persona, chatResponse.Text, cancellationToken);
                if (validation.IsValid && TryReadValidOutput(chatResponse.Text, dreamIds, out modelOutput))
                {
                    break;
                }
            }
        }
        catch (Exception exception)
        {
            failureKind = exception.GetType().Name;
        }

        if (modelOutput is null)
        {
            dbContext.AiCostLedger.Add(CreateAskLedger("failed", failureKind ?? "Validation", attempts, inputTokens, outputTokens, Stopwatch.GetElapsedTime(askStarted)));
            await dbContext.SaveChangesAsync(cancellationToken);
            return AskDreamsResult.Failure(StatusCodes.Status503ServiceUnavailable, "answer", "Dream DNA could not answer safely right now. Please try again.");
        }

        var linkedSources = sources.Where(source => modelOutput.ReferencedDreamIds.Contains(source.Id))
            .Select(source => new AskDreamSourceResponse(source.Id, DreamMapper.ReadSummary(source)!, source.OccurredAt, source.CreatedAt))
            .ToArray();
        dbContext.AiCostLedger.Add(CreateAskLedger("completed", null, attempts, inputTokens, outputTokens, Stopwatch.GetElapsedTime(askStarted)));
        await dbContext.SaveChangesAsync(cancellationToken);

        return AskDreamsResult.Success(new AskDreamsResponse(
            modelOutput.Answer,
            modelOutput.Observations,
            modelOutput.Caveat,
            linkedSources,
            sources.Length));
    }

    private AiCostLedgerRecord CreateEmbeddingLedger(EmbeddingResult result, string status, string? failureKind, TimeSpan latency) => new()
    {
        UserSubject = currentUser.Subject,
        Provider = result.Provider,
        Model = result.Model,
        PersonaId = PersonaId,
        OperationType = "dream.query-embedding",
        Status = status,
        FailureKind = failureKind,
        AttemptCount = 1,
        InputTokens = result.InputTokens,
        TotalTokens = result.InputTokens,
        LatencyMilliseconds = Math.Max(0, (long)latency.TotalMilliseconds),
        EstimatedCostUsd = (result.InputTokens ?? 0) * embeddingOptions.Value.InputCostPerMillionTokensUsd / 1_000_000m
    };

    private AiCostLedgerRecord CreateFailedEmbeddingLedger(Exception exception, TimeSpan latency) => new()
    {
        UserSubject = currentUser.Subject,
        Provider = "Amazon Bedrock",
        Model = embeddingOptions.Value.Model,
        PersonaId = PersonaId,
        OperationType = "dream.query-embedding",
        Status = "failed",
        FailureKind = exception.GetType().Name,
        AttemptCount = 1,
        LatencyMilliseconds = Math.Max(0, (long)latency.TotalMilliseconds),
        EstimatedCostUsd = 0
    };

    private AiCostLedgerRecord CreateAskLedger(string status, string? failureKind, int attempts, int inputTokens, int outputTokens, TimeSpan latency) => new()
    {
        UserSubject = currentUser.Subject,
        Provider = "DeepSeek",
        Model = deepSeekOptions.Value.Model,
        PersonaId = PersonaId,
        OperationType = "dream.ask",
        Status = status,
        FailureKind = failureKind,
        AttemptCount = attempts,
        InputTokens = inputTokens,
        OutputTokens = outputTokens,
        TotalTokens = inputTokens + outputTokens,
        LatencyMilliseconds = Math.Max(0, (long)latency.TotalMilliseconds),
        EstimatedCostUsd = inputTokens * usageCostOptions.Value.InputCostPerMillionTokens / 1_000_000m
            + outputTokens * usageCostOptions.Value.OutputCostPerMillionTokens / 1_000_000m
    };

    private static bool TryReadValidOutput(string json, IReadOnlyCollection<Guid> allowedIds, out AskModelOutput? output)
    {
        try
        {
            output = JsonSerializer.Deserialize<AskModelOutput>(json, JsonOptions);
            return output is not null
                && output.ReferencedDreamIds.Length > 0
                && output.ReferencedDreamIds.All(allowedIds.Contains);
        }
        catch (JsonException)
        {
            output = null;
            return false;
        }
    }

    private static AskDreamsResult MemoryUnavailable() => AskDreamsResult.Failure(
        StatusCodes.Status503ServiceUnavailable,
        "memory",
        "Your semantic dream memory is not ready yet. Try again after your dream history has been indexed.");

    private static int ToInt(long? value) => value is null ? 0 : checked((int)value.Value);

    private sealed record AskModelOutput(
        string Answer,
        string[] Observations,
        string Caveat,
        Guid[] ReferencedDreamIds);
}
