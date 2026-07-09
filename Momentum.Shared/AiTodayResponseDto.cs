namespace Momentum.Shared;

// Response for GET /api/ai/today (AI-001). AI-safe: no notes/journal text, no UserId,
// no activity/log IDs, no CreatedAt, no user profile data.
public class AiTodayResponseDto
{
    public DateOnly Date { get; set; }
    public int TotalPoints { get; set; }
    public int EntryCount { get; set; }
    public List<AiTodayEntryDto> Entries { get; set; } = [];
}
