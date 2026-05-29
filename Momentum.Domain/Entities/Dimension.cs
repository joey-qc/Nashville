namespace Momentum.Domain.Entities;

public class Dimension
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;

    public ICollection<ActivityDimension> ActivityDimensions { get; set; } = [];
    public ICollection<ActivityLogEntryDimension> LogEntryDimensions { get; set; } = [];
}
