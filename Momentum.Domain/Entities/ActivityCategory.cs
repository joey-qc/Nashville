namespace Momentum.Domain.Entities;

public class ActivityCategory
{
    public int ActivityId { get; set; }
    public int CategoryId { get; set; }

    public Activity Activity { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
