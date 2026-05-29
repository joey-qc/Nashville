namespace Momentum.Domain.Entities;

public class ActivityLog
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int ActivityId { get; set; }
    public DateTime LoggedAt { get; set; }
    public int PointsRecorded { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public Activity Activity { get; set; } = null!;
    public ICollection<ActivityLogEntryDimension> LogEntryDimensions { get; set; } = [];
}
