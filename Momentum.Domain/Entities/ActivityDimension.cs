namespace Momentum.Domain.Entities;

public class ActivityDimension
{
    public int ActivityId { get; set; }
    public int DimensionId { get; set; }

    public Activity Activity { get; set; } = null!;
    public Dimension Dimension { get; set; } = null!;
}
