namespace Momentum.Shared;

public class DailyScoreDto
{
    public DateOnly Date { get; set; }
    public int Total { get; set; }
    public Dictionary<int, int> ByCategory { get; set; } = [];
}
