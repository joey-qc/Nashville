using Momentum.Application.Interfaces;
using Momentum.Application.Services;
using Momentum.Domain.Entities;
using Momentum.Shared;
using NSubstitute;

namespace Momentum.Tests;

public class ActivityServiceTests
{
    private readonly IActivityRepository _activityRepo = Substitute.For<IActivityRepository>();
    private readonly IActivityLogRepository _logRepo = Substitute.For<IActivityLogRepository>();
    private readonly ActivityService _sut;
    private const string UserId = "user-1";

    public ActivityServiceTests() => _sut = new ActivityService(_activityRepo, _logRepo);

    [Fact]
    public async Task DeleteAsync_NoLogs_DeletesActivity()
    {
        var activity = new Activity { Id = 1, UserId = UserId, Categories = [] };
        _activityRepo.GetByIdAsync(1, UserId).Returns(activity);
        _logRepo.CountByActivityAsync(1, UserId).Returns(0);

        var (deleted, logCount) = await _sut.DeleteAsync(1, UserId, null);

        Assert.True(deleted);
        Assert.Equal(0, logCount);
    }

    [Fact]
    public async Task DeleteAsync_HasLogs_NoAction_Returns409Info()
    {
        var activity = new Activity { Id = 1, UserId = UserId, Categories = [] };
        _activityRepo.GetByIdAsync(1, UserId).Returns(activity);
        _logRepo.CountByActivityAsync(1, UserId).Returns(3);

        var (deleted, logCount) = await _sut.DeleteAsync(1, UserId, null);

        Assert.False(deleted);
        Assert.Equal(3, logCount);
    }

    [Fact]
    public async Task DeleteAsync_ArchiveAction_SetsIsArchived()
    {
        var activity = new Activity { Id = 1, UserId = UserId, Categories = [], IsArchived = false };
        _activityRepo.GetByIdAsync(1, UserId).Returns(activity);
        _logRepo.CountByActivityAsync(1, UserId).Returns(2);

        var (deleted, _) = await _sut.DeleteAsync(1, UserId, "archive");

        Assert.True(deleted);
        Assert.True(activity.IsArchived);
        await _activityRepo.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ReturnsFalseZero()
    {
        _activityRepo.GetByIdAsync(99, UserId).Returns((Activity?)null);

        var (deleted, logCount) = await _sut.DeleteAsync(99, UserId, null);

        Assert.False(deleted);
        Assert.Equal(0, logCount);
    }

    [Fact]
    public async Task GetAllAsync_OnlyReturnsUserActivities()
    {
        var activities = new List<Activity>
        {
            new() { Id = 1, UserId = UserId, Name = "Run", Categories = [] },
            new() { Id = 2, UserId = UserId, Name = "Read", Categories = [] }
        };
        _activityRepo.GetAllAsync(UserId).Returns(activities);

        var result = (await _sut.GetAllAsync(UserId)).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, a => Assert.NotNull(a.Name));
    }

    [Fact]
    public async Task CreateAsync_MapsFieldsCorrectly()
    {
        var dto = new CreateActivityDto
        {
            Name = "Yoga",
            DefaultPoints = 6,
            CategoryIds = [2, 3]
        };

        Activity? captured = null;
        await _activityRepo.AddAsync(Arg.Do<Activity>(a => captured = a));

        await _sut.CreateAsync(UserId, dto);

        Assert.NotNull(captured);
        Assert.Equal("Yoga", captured.Name);
        Assert.Equal(UserId, captured.UserId);
        Assert.Equal(6, captured.DefaultPoints);
        Assert.Equal(2, captured.Categories.Count);
    }
}
