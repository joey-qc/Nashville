namespace Momentum.Shared;

public class ActivityLogDto
{
    public int Id { get; set; }
    public int ActivityId { get; set; }
    public string ActivityName { get; set; } = string.Empty;
    public List<CategoryDto> Categories { get; set; } = [];
    public DateTime LoggedAt { get; set; }
    public int PointsRecorded { get; set; }
    public string? Notes { get; set; }
}
