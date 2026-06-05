using Momentum.Application.Interfaces;
using Momentum.Application.Services;
using Momentum.Domain.Entities;
using Momentum.Shared;
using NSubstitute;

namespace Momentum.Tests;

public class ScoreServiceTests
{
    private readonly IActivityLogRepository _logRepo = Substitute.For<IActivityLogRepository>();
    private readonly ScoreService _sut;
    private const string UserId = "user-1";

    public ScoreServiceTests() => _sut = new ScoreService(_logRepo);

    [Fact]
    public async Task GetSummaryAsync_ReturnsCorrectTodayTotal()
    {
        var today = DateTime.UtcNow.Date;
        _logRepo.GetByDateRangeAsync(UserId, Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([
                new ActivityLog { UserId = UserId, LoggedAt = today.AddHours(8),  PointsRecorded = 5, Activity = new Activity() },
                new ActivityLog { UserId = UserId, LoggedAt = today.AddHours(10), PointsRecorded = 8, Activity = new Activity() }
            ]);

        var result = await _sut.GetSummaryAsync(UserId);

        Assert.Equal(13, result.TodayTotal);
    }

    [Fact]
    public async Task GetSummaryAsync_NegativePoints_ReducesToday()
    {
        var today = DateTime.UtcNow.Date;
        _logRepo.GetByDateRangeAsync(UserId, Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([
                new ActivityLog { UserId = UserId, LoggedAt = today.AddHours(8),  PointsRecorded =  7, Activity = new Activity() },
                new ActivityLog { UserId = UserId, LoggedAt = today.AddHours(23), PointsRecorded = -6, Activity = new Activity() }
            ]);

        var result = await _sut.GetSummaryAsync(UserId);

        Assert.Equal(1, result.TodayTotal);
    }

    [Fact]
    public async Task GetSummaryAsync_EmptyLogs_ReturnsZeroes()
    {
        _logRepo.GetByDateRangeAsync(UserId, Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([]);

        var result = await _sut.GetSummaryAsync(UserId);

        Assert.Equal(0, result.TodayTotal);
        Assert.Equal(0, result.WeekTotal);
        Assert.Equal(0, result.MonthTotal);
    }

    [Fact]
    public async Task GetSummaryAsync_WithLocalBoundaries_ExcludesEntryBeforeLocalMidnight()
    {
        // Simulate an Eastern user (UTC-4 in summer).
        // Local midnight May 31 = 04:00 UTC May 31.
        // An entry at 03:00 UTC is 11 PM EDT May 30 — belongs to yesterday locally.
        // An entry at 12:00 UTC is  8 AM EDT May 31 — belongs to today locally.
        var localTodayStartUtc = new DateTime(2026, 5, 31,  4, 0, 0, DateTimeKind.Utc);
        var weekStartUtc       = new DateTime(2026, 5, 26,  4, 0, 0, DateTimeKind.Utc); // prior Sunday midnight EDT
        var monthStartUtc      = new DateTime(2026, 5,  1,  4, 0, 0, DateTimeKind.Utc); // May 1 midnight EDT

        _logRepo.GetByDateRangeAsync(UserId, Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([
                // 11 PM EDT May 30 = 03:00 UTC May 31 — should NOT count as today
                new ActivityLog
                {
                    UserId = UserId,
                    LoggedAt = new DateTime(2026, 5, 31, 3, 0, 0, DateTimeKind.Utc),
                    PointsRecorded = 5,
                    Activity = new Activity()
                },
                // 8 AM EDT May 31 = 12:00 UTC May 31 — should count as today
                new ActivityLog
                {
                    UserId = UserId,
                    LoggedAt = new DateTime(2026, 5, 31, 12, 0, 0, DateTimeKind.Utc),
                    PointsRecorded = 10,
                    Activity = new Activity()
                }
            ]);

        var result = await _sut.GetSummaryAsync(UserId, localTodayStartUtc, weekStartUtc, monthStartUtc);

        // Only the 8 AM entry is on or after local midnight — TodayTotal = 10
        Assert.Equal(10, result.TodayTotal);
        // Both entries are within the week (May 26 04:00 UTC to now) — WeekTotal = 15
        Assert.Equal(15, result.WeekTotal);
        // Both entries are within May — MonthTotal = 15
        Assert.Equal(15, result.MonthTotal);
    }

    // KI-015 regression: weekly comparison chart should group by local day, not UTC day.
    [Fact]
    public async Task GetWeeklyComparisonAsync_WithOffset_BucketsLateEveningEntryUnderLocalSunday()
    {
        // Entry at Sunday 23:00 EDT (UTC-4) = Monday 03:00 UTC.
        // UTC grouping would place it in Monday; local grouping must place it in Sunday.
        const int offset = -240; // EDT (UTC-4)
        var localNow        = DateTime.UtcNow.AddMinutes(offset);
        var localSunday     = localNow.Date.AddDays(-(int)localNow.DayOfWeek);
        // Convert Sunday 23:00 local to UTC: local + abs(offset) minutes
        var sunday23hUtc    = localSunday.AddHours(23).AddMinutes(-offset);

        _logRepo.GetByDateRangeAsync(UserId, Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([
                new ActivityLog
                {
                    UserId         = UserId,
                    LoggedAt       = sunday23hUtc, // Monday 03:00 UTC, Sunday 23:00 local
                    PointsRecorded = 10,
                    Activity       = new Activity()
                }
            ]);

        var result = await _sut.GetWeeklyComparisonAsync(UserId, localOffsetMinutes: offset);

        // Should appear in Sunday's bucket (local day), not Monday's (UTC day)
        Assert.Equal(10, result.Days.First(d => d.DayLabel == "Sun").CurrentWeek);
        Assert.Equal(0,  result.Days.First(d => d.DayLabel == "Mon").CurrentWeek);
    }

    // KI-015 regression: daily Trends chart should group by local day, not UTC day.
    [Fact]
    public async Task GetDailyTotalsAsync_WithOffset_BucketsLateEveningEntryUnderLocalDay()
    {
        // Entry at yesterday 23:00 local EDT = today 03:00 UTC.
        // UTC grouping would place it in today; local grouping must place it under yesterday.
        const int offset   = -240; // EDT (UTC-4)
        var localNow       = DateTime.UtcNow.AddMinutes(offset);
        var localYesterday = localNow.Date.AddDays(-1);
        // Convert yesterday 23:00 local to UTC
        var yesterday23hUtc = localYesterday.AddHours(23).AddMinutes(-offset);

        _logRepo.GetByDateRangeAsync(UserId, Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([
                new ActivityLog
                {
                    UserId         = UserId,
                    LoggedAt       = yesterday23hUtc, // today 03:00 UTC, yesterday 23:00 local
                    PointsRecorded = 10,
                    Activity       = new Activity()
                }
            ]);

        var result = (await _sut.GetDailyTotalsAsync(UserId, 7, null, localOffsetMinutes: offset)).ToList();

        var expectedDate = DateOnly.FromDateTime(localYesterday);
        var yesterdayBucket = result.FirstOrDefault(r => r.Date == expectedDate);
        Assert.NotNull(yesterdayBucket);
        Assert.Equal(10, yesterdayBucket.Total);

        // Must NOT appear in today's bucket
        var todayDate   = DateOnly.FromDateTime(localNow.Date);
        var todayBucket = result.FirstOrDefault(r => r.Date == todayDate);
        Assert.True(todayBucket is null || todayBucket.Total == 0,
            "Entry logged at 23:00 local should not appear in today's bucket.");
    }
}
