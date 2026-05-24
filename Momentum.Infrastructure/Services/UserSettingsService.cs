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
            Email = user.Email ?? string.Empty
        };
    }

    public async Task<UserSettingsDto?> UpdateAsync(string userId, UpdateUserSettingsDto dto)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return null;

        user.DisplayName = dto.DisplayName;
        await userManager.UpdateAsync(user);

        return new UserSettingsDto
        {
            DisplayName = user.DisplayName,
            Email = user.Email ?? string.Empty
        };
    }
}
