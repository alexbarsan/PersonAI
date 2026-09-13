using System.Text.Json;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.Content;

public sealed class DailyDreamContentSeeder(DreamLensDbContext dbContext)
{
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

    public async Task EnsureCoverageAsync(DateOnly requestedDate, CancellationToken cancellationToken)
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
                CreatedAt = DateTimeOffset.UtcNow
            });
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
}
