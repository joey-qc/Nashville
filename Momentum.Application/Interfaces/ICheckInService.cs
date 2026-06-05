using Momentum.Shared;

namespace Momentum.Application.Interfaces;

public interface ICheckInService
{
    Task<IEnumerable<CheckInDto>> GetByDateRangeAsync(string userId, DateTime from, DateTime to);
    Task<CheckInDto?> GetByIdAsync(int id, string userId);

    /// <exception cref="ArgumentException">
    ///   Thrown when a score is outside [-5, 5], or when <c>ActivityLogId</c> is provided
    ///   but does not belong to the requesting user.
    /// </exception>
    Task<CheckInDto> CreateAsync(string userId, CreateCheckInRequestDto dto);

    /// <returns>Updated DTO, or <c>null</c> if the Check-In was not found.</returns>
    /// <exception cref="ArgumentException">Same conditions as CreateAsync.</exception>
    Task<CheckInDto?> UpdateAsync(int id, string userId, UpdateCheckInRequestDto dto);

    Task<bool> DeleteAsync(int id, string userId);
}
