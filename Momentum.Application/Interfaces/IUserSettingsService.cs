using Momentum.Shared;

namespace Momentum.Application.Interfaces;

public interface IUserSettingsService
{
    Task<UserSettingsDto?> GetAsync(string userId);
    Task<UserSettingsDto?> UpdateAsync(string userId, UpdateUserSettingsDto dto);
}
