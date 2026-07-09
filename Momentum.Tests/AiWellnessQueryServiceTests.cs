using Momentum.Application.Interfaces;
using Momentum.Application.Services;
using Momentum.Domain.Entities;
using Momentum.Shared;
using NSubstitute;

namespace Momentum.Tests;

public class AiWellnessQueryServiceTests
{
    private readonly IActivityLogRepository _logRepo = Substitute.For<IActivityLogRepository>();
    private readonly AiWellnessQueryService _sut;
    private const string UserId = "user-1";

    public AiWellnessQueryServiceTests() => _sut = new AiWellnessQueryService(_logRepo);

    [Fact]
    public async Task GetTodayAsync_ComputesTotalPointsAndEntryCount()
    {
        var today = DateTime.UtcNow.Date;
        var body = new Dimension { Id = 1, Name = "Body", ColorHex = "#76E04A" };
        _logRepo.GetByDateRangeAsync(UserId, Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([
                new ActivityLog
                {
                    UserId = UserId, LoggedAt = today.AddHours(8), PointsRecorded = 5,
                    Activity = new Activity { Name = "Exercise / Gym" },
                    LogEntryDimensions = [new ActivityLogEntryDimension { DimensionId = 1, Dimension = body }]
                },
                new ActivityLog
                {
                    UserId = UserId, LoggedAt = today.AddHours(10), PointsRecorded = 8,
                    Activity = new Activity { Name = "Meditation" },
                    LogEntryDimensions = []
                }
            ]);

        var result = await _sut.GetTodayAsync(UserId);

        Assert.Equal(13, result.TotalPoints);
        Assert.Equal(2, result.EntryCount);
        Assert.Equal(2, result.Entries.Count);
    }

    [Fact]
    public async Task GetTodayAsync_EmptyLogs_ReturnsZeroesAndEmptyEntries()
    {
        _logRepo.GetByDateRangeAsync(UserId, Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns([]);

        var result = await _sut.GetTodayAsync(UserId);

        Assert.Equal(0, result.TotalPoints);
        Assert.Equal(0, result.EntryCount);
        Assert.Empty(result.Entries);
    }

    [Fact]
    public async Task GetTodayAsync_IncludesDimensionNames()
    {
        var today = DateTime.UtcNow.Date;
        var mind = new Dimension { Id = 2, Name = "Mind", ColorHex = "#5BC8FF" };
        var spirit = new Dimension { Id = 3, Name = "Spirit", ColorHex = "#B894FF" };
        _logRepo.GetByDateRangeAsync(UserId, Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([
                new ActivityLog
                {
                    UserId = UserId, LoggedAt = today.AddHours(9), PointsRecorded = 10,
                    Activity = new Activity { Name = "Journaling" },
                    LogEntryDimensions =
                    [
                        new ActivityLogEntryDimension { DimensionId = 2, Dimension = mind },
                        new ActivityLogEntryDimension { DimensionId = 3, Dimension = spirit }
                    ]
                }
            ]);

        var result = await _sut.GetTodayAsync(UserId);

        var entry = Assert.Single(result.Entries);
        Assert.Equal(["Mind", "Spirit"], entry.Dimensions);
        Assert.Equal("Journaling", entry.ActivityName);
        Assert.Equal(10, entry.Points);
    }

    [Fact]
    public async Task GetTodayAsync_ResponseShape_ExposesOnlyAiSafeFields()
    {
        var today = DateTime.UtcNow.Date;
        _logRepo.GetByDateRangeAsync(UserId, Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([
                new ActivityLog
                {
                    Id = 999, UserId = UserId, ActivityId = 55,
                    LoggedAt = today.AddHours(8), PointsRecorded = 5,
                    Notes = "<p>Private journal text that must never reach the AI API.</p>",
                    CreatedAt = today.AddHours(8),
                    Activity = new Activity { Id = 55, Name = "Exercise / Gym" },
                    LogEntryDimensions = []
                }
            ]);

        var result = await _sut.GetTodayAsync(UserId);

        // Structural guarantee: the DTO types themselves carry no Notes/UserId/Id/ActivityId/
        // CreatedAt property, so the exclusion holds even if a future edit is careless about
        // what gets mapped in — not just "this particular value wasn't copied over."
        var entryProps = typeof(AiTodayEntryDto).GetProperties().Select(p => p.Name).ToArray();
        Assert.Equal(["LoggedAt", "ActivityName", "Points", "Dimensions"], entryProps);

        var responseProps = typeof(AiTodayResponseDto).GetProperties().Select(p => p.Name).ToArray();
        Assert.Equal(["Date", "TotalPoints", "EntryCount", "Entries"], responseProps);

        Assert.Equal("Exercise / Gym", Assert.Single(result.Entries).ActivityName);
    }

    [Theory]
    [InlineData(0)]     // UTC
    [InlineData(-240)]  // EDT (UTC-4)
    [InlineData(330)]   // IST (UTC+5:30)
    public async Task GetTodayAsync_WithLocalOffset_QueriesRepositoryWithCorrectUtcDayBoundaries(int offset)
    {
        _logRepo.GetByDateRangeAsync(UserId, Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns([]);

        await _sut.GetTodayAsync(UserId, offset);

        // Same boundary math as ScoreService's local-day pattern: local midnight, converted to UTC.
        var localToday = DateTime.UtcNow.AddMinutes(offset).Date;
        var expectedFrom = localToday.AddMinutes(-offset);
        var expectedTo = localToday.AddDays(1).AddMinutes(-offset);

        await _logRepo.Received(1).GetByDateRangeAsync(UserId,
            Arg.Is<DateTime>(d => d == expectedFrom),
            Arg.Is<DateTime>(d => d == expectedTo));
    }

    [Fact]
    public async Task GetTodayAsync_NoOffsetProvided_DefaultsToUtcDayBoundaries()
    {
        _logRepo.GetByDateRangeAsync(UserId, Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns([]);

        var result = await _sut.GetTodayAsync(UserId);

        var utcToday = DateTime.UtcNow.Date;
        await _logRepo.Received(1).GetByDateRangeAsync(UserId,
            Arg.Is<DateTime>(d => d == utcToday),
            Arg.Is<DateTime>(d => d == utcToday.AddDays(1)));
        Assert.Equal(DateOnly.FromDateTime(utcToday), result.Date);
    }
}
