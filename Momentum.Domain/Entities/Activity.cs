namespace Momentum.Domain.Entities;

public class Activity
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DefaultPoints { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ActivityCategory> Categories { get; set; } = [];
    public ICollection<ActivityLog> Logs { get; set; } = [];
}
