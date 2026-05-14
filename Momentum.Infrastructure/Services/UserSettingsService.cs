using Microsoft.AspNetCore.Identity;
using Momentum.Application.Interfaces;
using Momentum.Infrastructure.Identity;
using Momentum.Shared;

namespace Momentum.Infrastructure.Services;

public class UserSettingsService(UserManager<ApplicationUser> userManager) : IUserSettingsService
{
    public async Task<UserSettingsDto?> GetAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return null;

        return new UserSettingsDto
        {
            DisplayName = user.DisplayName,
            Email = user.Email ?? string.Empty,
            Theme = user.Theme
        };
    }

    public async Task<UserSettingsDto?> UpdateAsync(string userId, UpdateUserSettingsDto dto)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return null;

        user.DisplayName = dto.DisplayName;
        user.Theme = dto.Theme;
        await userManager.UpdateAsync(user);

        return new UserSettingsDto
        {
            DisplayName = user.DisplayName,
            Email = user.Email ?? string.Empty,
            Theme = user.Theme
        };
    }
}
