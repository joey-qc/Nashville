using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Momentum.API.Controllers;
using Momentum.Application.Interfaces;
using Momentum.Infrastructure.Identity;
using Momentum.Shared;
using NSubstitute;

namespace Momentum.Tests;

public class AiControllerTests
{
    private readonly IAiWellnessQueryService _aiService = Substitute.For<IAiWellnessQueryService>();
    private readonly ILogger<AiController> _logger = Substitute.For<ILogger<AiController>>();
    private const string ValidKey = "test-ai-key-12345";
    private const string ConfiguredEmail = "ai-user@example.com";

    private static UserManager<ApplicationUser> MockUserManager()
    {
        var store = Substitute.For<IUserStore<ApplicationUser>>();
        return Substitute.For<UserManager<ApplicationUser>>(store, null, null, null, null, null, null, null, null);
    }

    private static IConfiguration BuildConfig(string? apiKey = ValidKey, string? userEmail = ConfiguredEmail, string? offset = null)
    {
        var dict = new Dictionary<string, string?>();
        if (apiKey is not null) dict["Ai:ApiKey"] = apiKey;
        if (userEmail is not null) dict["Ai:UserEmail"] = userEmail;
        if (offset is not null) dict["Ai:DefaultLocalOffsetMinutes"] = offset;
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private AiController BuildController(UserManager<ApplicationUser> userManager, IConfiguration configuration, string? headerValue)
    {
        var controller = new AiController(_aiService, userManager, configuration, _logger);
        var httpContext = new DefaultHttpContext();
        if (headerValue is not null)
            httpContext.Request.Headers["X-Momentum-AI-Key"] = headerValue;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    // ── Unauthorized behavior ──────────────────────────────────────────────────

    [Fact]
    public async Task GetToday_MissingApiKeyHeader_ReturnsUnauthorized()
    {
        var controller = BuildController(MockUserManager(), BuildConfig(), headerValue: null);

        var result = await controller.GetToday();

        Assert.IsType<UnauthorizedResult>(result);
        await _aiService.DidNotReceive().GetTodayAsync(Arg.Any<string>(), Arg.Any<int?>());
    }

    [Fact]
    public async Task GetToday_WrongApiKeyHeader_ReturnsUnauthorized()
    {
        var controller = BuildController(MockUserManager(), BuildConfig(), headerValue: "wrong-key");

        var result = await controller.GetToday();

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetToday_ApiKeyNotConfigured_Returns500AndDoesNotLeakKeyState()
    {
        var controller = BuildController(MockUserManager(), BuildConfig(apiKey: null), headerValue: ValidKey);

        var result = await controller.GetToday();

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    [Fact]
    public async Task GetToday_UserEmailNotConfigured_Returns500()
    {
        var controller = BuildController(MockUserManager(), BuildConfig(userEmail: null), headerValue: ValidKey);

        var result = await controller.GetToday();

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    [Fact]
    public async Task GetToday_ConfiguredUserNotFound_Returns500()
    {
        var userManager = MockUserManager();
        userManager.FindByEmailAsync(ConfiguredEmail).Returns((ApplicationUser?)null);
        var controller = BuildController(userManager, BuildConfig(), headerValue: ValidKey);

        var result = await controller.GetToday();

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    // ── Authorized behavior ────────────────────────────────────────────────────

    [Fact]
    public async Task GetToday_ValidKeyAndConfiguredUser_ReturnsOkWithServiceResult()
    {
        var user = new ApplicationUser { Id = "ai-user-id", Email = ConfiguredEmail };
        var userManager = MockUserManager();
        userManager.FindByEmailAsync(ConfiguredEmail).Returns(user);
        var expected = new AiTodayResponseDto { TotalPoints = 20, EntryCount = 2 };
        _aiService.GetTodayAsync("ai-user-id", Arg.Any<int?>()).Returns(expected);

        var controller = BuildController(userManager, BuildConfig(), headerValue: ValidKey);

        var result = await controller.GetToday();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task GetToday_ValidKey_ResolvesConfiguredUser_NotAnArbitraryCaller()
    {
        var user = new ApplicationUser { Id = "ai-user-id", Email = ConfiguredEmail };
        var userManager = MockUserManager();
        userManager.FindByEmailAsync(ConfiguredEmail).Returns(user);
        _aiService.GetTodayAsync(Arg.Any<string>(), Arg.Any<int?>()).Returns(new AiTodayResponseDto());

        var controller = BuildController(userManager, BuildConfig(), headerValue: ValidKey);
        await controller.GetToday();

        await _aiService.Received(1).GetTodayAsync("ai-user-id", Arg.Any<int?>());
    }

    [Fact]
    public async Task GetToday_ExplicitQueryOffset_TakesPrecedenceOverConfiguredDefault()
    {
        var user = new ApplicationUser { Id = "ai-user-id", Email = ConfiguredEmail };
        var userManager = MockUserManager();
        userManager.FindByEmailAsync(ConfiguredEmail).Returns(user);
        _aiService.GetTodayAsync(Arg.Any<string>(), Arg.Any<int?>()).Returns(new AiTodayResponseDto());

        var controller = BuildController(userManager, BuildConfig(offset: "-240"), headerValue: ValidKey);
        await controller.GetToday(localOffsetMinutes: 330);

        await _aiService.Received(1).GetTodayAsync("ai-user-id", 330);
    }

    [Fact]
    public async Task GetToday_NoExplicitOffset_FallsBackToConfiguredDefault()
    {
        var user = new ApplicationUser { Id = "ai-user-id", Email = ConfiguredEmail };
        var userManager = MockUserManager();
        userManager.FindByEmailAsync(ConfiguredEmail).Returns(user);
        _aiService.GetTodayAsync(Arg.Any<string>(), Arg.Any<int?>()).Returns(new AiTodayResponseDto());

        var controller = BuildController(userManager, BuildConfig(offset: "-240"), headerValue: ValidKey);
        await controller.GetToday();

        await _aiService.Received(1).GetTodayAsync("ai-user-id", -240);
    }
}
