using System.Text.Json;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.Content;

public sealed class DailyDreamContentSeeder(DreamLensDbContext dbContext)
{
    private const long SeedLockId = 0x445245414D444E41;

    private static readonly string[] Quotes =
    [
        "In winter, even a quiet dream can be a small lantern.",
        "A dream is a note from the part of you that speaks in images.",
        "What returns at night may be asking only to be noticed.",
        "A remembered fragment can still lead somewhere meaningful.",
        "Dreams do not have to be solved to be worth keeping.",
        "The mind rehearses, wanders, and makes unlikely companions after dark.",
        "A recurring place in a dream can become a familiar room in your inner map.",
        "Some dreams arrive as stories. Others arrive as weather.",
        "The details that linger often deserve a line in your journal.",
        "A dream can be strange without being a warning.",
        "Sleep gives memory, emotion, and imagination a little room to rearrange.",
        "The night does not always explain itself, but it often leaves a trace."
    ];

    private static readonly string[] Facts =
    [
        "Most people dream several times a night, even when they remember none of them in the morning.",
        "Dream recall is often strongest when you wake during or just after a dream-rich period of sleep.",
        "Vivid dreams are common in REM sleep, though dreaming can happen in other sleep stages too.",
        "Dreams often combine familiar people, places, and concerns in new arrangements.",
        "A dream can feel long even when the remembered sequence is brief.",
        "Writing down even one image after waking can make later recall easier.",
        "Emotions in dreams can be more memorable than the plot itself.",
        "Recurring dreams can repeat a feeling, setting, or situation rather than an identical story.",
        "People frequently recognize places in dreams that do not exist as one real location.",
        "Dreams may include ordinary details, not only dramatic symbols or surreal scenes.",
        "A dream journal can reveal patterns that are difficult to notice from one dream alone.",
        "Falling, flying, arriving late, and returning home are all common dream themes across many people.",
        "Dreams can borrow a face, a room, or a conversation from something encountered days earlier.",
        "Not remembering a dream does not mean you did not dream.",
        "Stress, routines, sleep timing, and new experiences can all influence what people remember dreaming about.",
        "Dreams are not reliable predictions, but they can be useful material for reflection.",
        "A short title can make a dream easier to find when a similar theme returns later.",
        "The same dream image can carry different associations for different people.",
        "Dream recall can improve simply by pausing for a moment before reaching for a phone after waking.",
        "A dream does not need a complete ending to be worth recording.",
        "The brain is highly capable of making a dream feel coherent while you are inside it.",
        "Dream characters can feel familiar even when they are composites of several people.",
        "A change in perspective, place, or time is ordinary in dream storytelling.",
        "Reviewing old entries can help distinguish a one-off image from a recurring pattern."
    ];

    // Editorial, non-diagnostic facts paraphrased from the references in docs/content-sources.md.
    private static readonly string[] CognitiveFacts =
    [
        "Sleep is an active brain state: research links it with the strengthening and reorganization of recently formed memories.",
        "The hippocampus helps form and retrieve memories; it works with many other brain regions rather than storing a whole experience by itself.",
        "Memory is reconstructive. Remembering an event can involve rebuilding it from fragments, context, and prior knowledge.",
        "REM is one sleep stage in which dreams are often especially vivid, but dream experiences can occur in other stages too.",
        "Attention is selective: a dream detail that stands out may be salient to you without carrying one universal meaning.",
        "Emotion processing is distributed across brain networks, including systems involved in detecting relevance and regulating response.",
        "Cognitive reappraisal means reconsidering how a situation is interpreted. It is a skill, not a verdict about what a dream means.",
        "A remembered dream is a report from waking memory, so its details can shift as you recall and retell it.",
        "Sleep cycles through non-REM and REM stages; the sequence repeats across the night rather than staying in one state.",
        "Associative thinking links related ideas, memories, and sensations. Dreams can combine these elements in unfamiliar ways.",
        "The brain can create a strong sense of story and place even when a dream changes scene, time, or perspective quickly.",
        "Emotional tone can be easier to remember than a dream's sequence of events, which is one reason a brief note can be useful.",
        "A cognitive analysis can describe possible patterns in attention, memory, and emotion. It cannot diagnose a condition from a dream.",
        "Sleep research studies groups and probabilities, so one person's dream cannot confirm a general finding on its own.",
        "Recent experiences can influence later dream reports, but a dream is not a literal recording of the day.",
        "Context changes interpretation: the same image can bring different associations for different people and at different times.",
        "Reflection can separate observation from inference: noting what happened in a dream is different from deciding what it signifies.",
        "The brain's memory systems keep changing after an event, and sleep is one period associated with that ongoing processing.",
        "A recurring image can be a useful journal pattern to observe over time without assuming it predicts anything.",
        "Dream recall varies widely between people and nights; low recall is not evidence that dreaming did not occur.",
        "Psychological terms such as attention, memory, and emotion describe processes. They are not labels for a person's character.",
        "A cognitive perspective asks how a dream was experienced and remembered, not whether its symbols have a fixed dictionary meaning.",
        "Sleep and dreaming science continues to evolve, so Dream DNA presents these facts as context for reflection, not clinical advice.",
        "A short record soon after waking can preserve sensory details before ordinary morning activity competes for attention."
    ];

    public async Task EnsureCoverageAsync(DateOnly requestedDate, CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsNpgsql())
        {
            await EnsureCoverageCoreAsync(requestedDate, cancellationToken);
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({SeedLockId})",
            cancellationToken);
        await EnsureCoverageCoreAsync(requestedDate, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task EnsureCoverageCoreAsync(DateOnly requestedDate, CancellationToken cancellationToken)
    {
        var firstDate = requestedDate.AddDays(-30);
        var lastDate = requestedDate.AddDays(395);
        var existingDates = await dbContext.DailyDreamContent
            .Where(content => content.ContentDate >= firstDate && content.ContentDate <= lastDate)
            .Select(content => content.ContentDate)
            .ToListAsync(cancellationToken);
        var existing = existingDates.ToHashSet();

        for (var date = firstDate; date <= lastDate; date = date.AddDays(1))
        {
            if (existing.Contains(date))
            {
                continue;
            }

            dbContext.DailyDreamContent.Add(new DailyDreamContent
            {
                ContentDate = date,
                Quote = Quotes[(date.Month - 1) % Quotes.Length],
                Attribution = "Dream DNA editorial",
                FactsJson = JsonSerializer.Serialize(SelectFacts(date)),
                CognitiveFactsJson = JsonSerializer.Serialize(SelectCognitiveFacts(date)),
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        var contentWithoutCognitiveFacts = await dbContext.DailyDreamContent
            .Where(content => content.ContentDate >= firstDate
                && content.ContentDate <= lastDate
                && string.IsNullOrWhiteSpace(content.CognitiveFactsJson))
            .ToListAsync(cancellationToken);
        foreach (var content in contentWithoutCognitiveFacts)
        {
            content.CognitiveFactsJson = JsonSerializer.Serialize(SelectCognitiveFacts(content.ContentDate));
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static string[] SelectFacts(DateOnly date)
    {
        var start = Math.Abs(date.DayNumber) % Facts.Length;
        return Enumerable.Range(0, 3)
            .Select(offset => Facts[(start + offset * 7) % Facts.Length])
            .ToArray();
    }

    private static string[] SelectCognitiveFacts(DateOnly date)
    {
        var start = Math.Abs(date.DayNumber * 3) % CognitiveFacts.Length;
        return Enumerable.Range(0, 3)
            .Select(offset => CognitiveFacts[(start + offset * 5) % CognitiveFacts.Length])
            .ToArray();
    }
}
