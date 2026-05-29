namespace Momentum.Domain.Entities;

public class ActivityLogEntryDimension
{
    public int ActivityLogId { get; set; }
    public int DimensionId { get; set; }

    public ActivityLog ActivityLog { get; set; } = null!;
    public Dimension Dimension { get; set; } = null!;
}
