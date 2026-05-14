namespace Momentum.Shared;

public class WeeklyComparisonDto
{
    public List<DayComparisonDto> Days { get; set; } = [];
}

public class DayComparisonDto
{
    public string DayLabel { get; set; } = string.Empty;
    public int CurrentWeek { get; set; }
    public int LastWeek { get; set; }
}
