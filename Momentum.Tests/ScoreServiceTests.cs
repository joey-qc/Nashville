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
}
