namespace Momentum.Shared;

// AI-safe projection of a single activity log entry (AI-001). Intentionally excludes
// Notes, UserId, ActivityId, log Id, and CreatedAt — only display-safe fields are exposed.
public class AiTodayEntryDto
{
    public DateTime LoggedAt { get; set; }
    public string ActivityName { get; set; } = string.Empty;
    public int Points { get; set; }
    public List<string> Dimensions { get; set; } = [];
}
