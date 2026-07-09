using Momentum.Shared;

namespace Momentum.Application.Interfaces;

public interface IAiWellnessQueryService
{
    Task<AiTodayResponseDto> GetTodayAsync(string userId, int? localOffsetMinutes = null);
}
