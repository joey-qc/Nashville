namespace Momentum.Application.Interfaces;

public interface IActivitySeedService
{
    Task SeedDefaultActivitiesAsync(string userId);
}
