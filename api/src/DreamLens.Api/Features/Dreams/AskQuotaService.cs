using System.Data;
using DreamLens.Api.Infrastructure.Monetization;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Features.Dreams;

public interface IAskQuotaService
{
    Task<AskQuotaState> GetStateAsync(string userSubject, string timezone, EntitlementSnapshot entitlement, CancellationToken cancellationToken);
    Task<AskQuotaReservation> TryReserveAsync(string userSubject, string timezone, EntitlementSnapshot entitlement, CancellationToken cancellationToken);
    Task CompleteAsync(Guid reservationId, CancellationToken cancellationToken);
    Task ReleaseAsync(Guid reservationId, CancellationToken cancellationToken);
}

public sealed record AskQuotaState(int DailyLimit, int? Remaining, DateTimeOffset? ResetsAt, bool IsExempt);

public sealed record AskQuotaReservation(Guid? Id, AskQuotaState State, bool Accepted);

public sealed class AskQuotaService(
    DreamLensDbContext dbContext,
    IOptions<AskDreamsOptions> options) : IAskQuotaService
{
    public async Task<AskQuotaState> GetStateAsync(
        string userSubject,
        string timezone,
        EntitlementSnapshot entitlement,
        CancellationToken cancellationToken)
    {
        var limit = GetLimit(entitlement);
        var window = AccountDayWindow.Create(timezone, DateTimeOffset.UtcNow);
        if (entitlement.QuotaExempt)
        {
            return new AskQuotaState(limit, null, window.ResetsAt, true);
        }

        var used = await CountActiveReservationsAsync(userSubject, window, cancellationToken);
        return new AskQuotaState(limit, Math.Max(0, limit - used), window.ResetsAt, false);
    }

    public async Task<AskQuotaReservation> TryReserveAsync(
        string userSubject,
        string timezone,
        EntitlementSnapshot entitlement,
        CancellationToken cancellationToken)
    {
        var limit = GetLimit(entitlement);
        var window = AccountDayWindow.Create(timezone, DateTimeOffset.UtcNow);
        if (entitlement.QuotaExempt)
        {
            return new AskQuotaReservation(null, new AskQuotaState(limit, null, window.ResetsAt, true), true);
        }

        if (!dbContext.Database.IsRelational())
        {
            var used = await CountActiveReservationsAsync(userSubject, window, cancellationToken);
            if (used >= limit)
            {
                return new AskQuotaReservation(null, new AskQuotaState(limit, 0, window.ResetsAt, false), false);
            }

            var reservation = new AskQuestionUsageRecord
            {
                UserSubject = userSubject,
                AccountTimezone = window.Timezone,
                AccountLocalDate = window.LocalDate
            };
            dbContext.AskQuestionUsages.Add(reservation);
            await dbContext.SaveChangesAsync(cancellationToken);
            return new AskQuotaReservation(
                reservation.Id,
                new AskQuotaState(limit, Math.Max(0, limit - used - 1), window.ResetsAt, false),
                true);
        }

        for (var attempt = 0; attempt < 3; attempt++)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var used = await CountActiveReservationsAsync(userSubject, window, cancellationToken);
            if (used >= limit)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new AskQuotaReservation(null, new AskQuotaState(limit, 0, window.ResetsAt, false), false);
            }

            var reservation = new AskQuestionUsageRecord
            {
                UserSubject = userSubject,
                AccountTimezone = window.Timezone,
                AccountLocalDate = window.LocalDate
            };
            dbContext.AskQuestionUsages.Add(reservation);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return new AskQuotaReservation(
                    reservation.Id,
                    new AskQuotaState(limit, Math.Max(0, limit - used - 1), window.ResetsAt, false),
                    true);
            }
            catch (DbUpdateException) when (attempt < 2)
            {
                await transaction.RollbackAsync(cancellationToken);
                dbContext.Entry(reservation).State = EntityState.Detached;
            }
        }

        return new AskQuotaReservation(null, new AskQuotaState(limit, 0, window.ResetsAt, false), false);
    }

    public async Task CompleteAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        var reservation = await dbContext.AskQuestionUsages.SingleOrDefaultAsync(row => row.Id == reservationId, cancellationToken);
        if (reservation is null || reservation.Status != "reserved") return;
        reservation.Status = "completed";
        reservation.CompletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReleaseAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        var reservation = await dbContext.AskQuestionUsages.SingleOrDefaultAsync(row => row.Id == reservationId, cancellationToken);
        if (reservation is null || reservation.Status != "reserved") return;
        reservation.Status = "released";
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<int> CountActiveReservationsAsync(string userSubject, AccountDayWindow window, CancellationToken cancellationToken) =>
        await dbContext.AskQuestionUsages.CountAsync(
            row => row.UserSubject == userSubject
                && row.ReservedAt >= window.StartsAt
                && row.ReservedAt < window.ResetsAt
                && (row.Status == "reserved" || row.Status == "completed"),
            cancellationToken);

    private int GetLimit(EntitlementSnapshot entitlement) => entitlement.Tier == EntitlementTier.Premium
        ? Math.Max(0, options.Value.PremiumDailyLimit)
        : Math.Max(0, options.Value.FreeDailyLimit);

    private sealed record AccountDayWindow(string Timezone, DateOnly LocalDate, DateTimeOffset StartsAt, DateTimeOffset ResetsAt)
    {
        public static AccountDayWindow Create(string timezone, DateTimeOffset now)
        {
            var zone = TryGetTimezone(timezone);
            var localNow = TimeZoneInfo.ConvertTime(now, zone);
            var localDate = DateOnly.FromDateTime(localNow.DateTime);
            var startLocal = localDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            var resetLocal = localDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            return new AccountDayWindow(
                zone.Id,
                localDate,
                new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(startLocal, zone)),
                new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(resetLocal, zone)));
        }

        private static TimeZoneInfo TryGetTimezone(string timezone)
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(timezone); }
            catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; }
            catch (InvalidTimeZoneException) { return TimeZoneInfo.Utc; }
        }
    }
}
