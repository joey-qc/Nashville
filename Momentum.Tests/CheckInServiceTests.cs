using Momentum.Application.Interfaces;
using Momentum.Application.Services;
using Momentum.Domain.Entities;
using Momentum.Shared;
using NSubstitute;

namespace Momentum.Tests;

public class CheckInServiceTests
{
    private readonly ICheckInRepository    _checkInRepo = Substitute.For<ICheckInRepository>();
    private readonly IActivityLogRepository _logRepo    = Substitute.For<IActivityLogRepository>();
    private readonly CheckInService _sut;
    private const string UserId      = "user-1";
    private const string OtherUserId = "user-2";

    public CheckInServiceTests() => _sut = new CheckInService(_checkInRepo, _logRepo);

    // ── Create: happy path ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidScores_ReturnsDto()
    {
        var dto = new CreateCheckInRequestDto
        {
            BodyScore   =  2,
            EnergyScore = -1,
            MoodScore   =  0
        };
        _checkInRepo.AddAsync(Arg.Any<CheckIn>()).Returns(Task.CompletedTask);
        _checkInRepo.SaveChangesAsync().Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(UserId, dto);

        Assert.Equal(UserId, result.UserId);
        Assert.Equal(2,  result.BodyScore);
        Assert.Equal(-1, result.EnergyScore);
        Assert.Equal(0,  result.MoodScore);
        Assert.Null(result.ActivityLogId);
    }

    [Fact]
    public async Task CreateAsync_WithValidActivityLogId_LinksSuccessfully()
    {
        var dto = new CreateCheckInRequestDto
        {
            BodyScore = 1, EnergyScore = 1, MoodScore = 1,
            ActivityLogId = 42
        };
        _logRepo.GetByIdAsync(42, UserId)
            .Returns(new ActivityLog { Id = 42, UserId = UserId, Activity = new Activity() });
        _checkInRepo.AddAsync(Arg.Any<CheckIn>()).Returns(Task.CompletedTask);
        _checkInRepo.SaveChangesAsync().Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(UserId, dto);

        Assert.Equal(42, result.ActivityLogId);
    }

    // ── Create: score validation ──────────────────────────────────────────────

    [Theory]
    [InlineData( 6,  0,  0)]   // BodyScore too high
    [InlineData(-6,  0,  0)]   // BodyScore too low
    [InlineData( 0,  6,  0)]   // EnergyScore too high
    [InlineData( 0, -6,  0)]   // EnergyScore too low
    [InlineData( 0,  0,  6)]   // MoodScore too high
    [InlineData( 0,  0, -6)]   // MoodScore too low
    public async Task CreateAsync_ScoreOutOfRange_ThrowsArgumentException(int body, int energy, int mood)
    {
        var dto = new CreateCheckInRequestDto
        {
            BodyScore = body, EnergyScore = energy, MoodScore = mood
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(UserId, dto));

        await _checkInRepo.DidNotReceive().AddAsync(Arg.Any<CheckIn>());
    }

    // ── Create: ActivityLogId ownership ──────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ActivityLogIdBelongsToOtherUser_ThrowsArgumentException()
    {
        var dto = new CreateCheckInRequestDto
        {
            BodyScore = 0, EnergyScore = 0, MoodScore = 0,
            ActivityLogId = 99
        };
        // Repo returns null because log 99 belongs to a different user
        _logRepo.GetByIdAsync(99, UserId).Returns((ActivityLog?)null);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.CreateAsync(UserId, dto));

        Assert.Contains("99", ex.Message);
        await _checkInRepo.DidNotReceive().AddAsync(Arg.Any<CheckIn>());
    }

    [Fact]
    public async Task CreateAsync_ActivityLogIdNotFound_ThrowsArgumentException()
    {
        var dto = new CreateCheckInRequestDto
        {
            BodyScore = 0, EnergyScore = 0, MoodScore = 0,
            ActivityLogId = 999
        };
        _logRepo.GetByIdAsync(999, UserId).Returns((ActivityLog?)null);

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(UserId, dto));
    }

    // ── Date range query: user isolation ─────────────────────────────────────

    [Fact]
    public async Task GetByDateRangeAsync_ReturnsOnlyCurrentUsersCheckIns()
    {
        var from = DateTime.UtcNow.Date;
        var to   = from.AddDays(1);
        var userCheckIn = new CheckIn
        {
            Id = 1, UserId = UserId, CheckedInAt = from.AddHours(8),
            BodyScore = 1, EnergyScore = 1, MoodScore = 1,
            CreatedAt = DateTime.UtcNow
        };
        // Repository is already scoped by userId in the implementation;
        // mock returns only the current user's records as it would in production.
        _checkInRepo.GetByDateRangeAsync(UserId, from, to)
            .Returns([userCheckIn]);

        var results = (await _sut.GetByDateRangeAsync(UserId, from, to)).ToList();

        Assert.Single(results);
        Assert.Equal(UserId, results[0].UserId);
        // Verify the query was called with the correct userId (never the other user's id)
        await _checkInRepo.DidNotReceive().GetByDateRangeAsync(OtherUserId, Arg.Any<DateTime>(), Arg.Any<DateTime>());
    }

    // ── Update: user isolation ────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_CheckInBelongsToCurrentUser_Succeeds()
    {
        var existing = new CheckIn
        {
            Id = 5, UserId = UserId,
            CheckedInAt = DateTime.UtcNow,
            BodyScore = 0, EnergyScore = 0, MoodScore = 0,
            CreatedAt = DateTime.UtcNow
        };
        _checkInRepo.GetByIdAsync(5, UserId).Returns(existing);
        _checkInRepo.SaveChangesAsync().Returns(Task.CompletedTask);

        var dto = new UpdateCheckInRequestDto
        {
            CheckedInAt = DateTime.UtcNow,
            BodyScore = 3, EnergyScore = -2, MoodScore = 1
        };

        var result = await _sut.UpdateAsync(5, UserId, dto);

        Assert.NotNull(result);
        Assert.Equal(3,  result.BodyScore);
        Assert.Equal(-2, result.EnergyScore);
    }

    [Fact]
    public async Task UpdateAsync_CheckInBelongsToOtherUser_ReturnsNull()
    {
        // Repo returns null because the check-in doesn't belong to this user
        _checkInRepo.GetByIdAsync(5, UserId).Returns((CheckIn?)null);

        var dto = new UpdateCheckInRequestDto
        {
            CheckedInAt = DateTime.UtcNow,
            BodyScore = 0, EnergyScore = 0, MoodScore = 0
        };

        var result = await _sut.UpdateAsync(5, UserId, dto);

        Assert.Null(result);
        await _checkInRepo.DidNotReceive().SaveChangesAsync();
    }

    // ── Delete: user isolation ────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_CheckInBelongsToCurrentUser_ReturnsTrue()
    {
        _checkInRepo.DeleteAsync(7, UserId).Returns(true);

        var result = await _sut.DeleteAsync(7, UserId);

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_CheckInBelongsToOtherUser_ReturnsFalse()
    {
        // Repo returns false because the id/userId combination doesn't exist
        _checkInRepo.DeleteAsync(7, UserId).Returns(false);

        var result = await _sut.DeleteAsync(7, UserId);

        Assert.False(result);
    }
}
