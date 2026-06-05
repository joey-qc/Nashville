namespace Momentum.Domain.Entities;

public class CheckIn
{
    public int     Id            { get; set; }
    public string  UserId        { get; set; } = string.Empty;

    /// <summary>User-editable effective timestamp. Used for analytics, display, and sorting.</summary>
    public DateTime CheckedInAt  { get; set; }

    public int     BodyScore     { get; set; }
    public int     EnergyScore   { get; set; }
    public int     MoodScore     { get; set; }

    /// <summary>Optional link to the ActivityLog this check-in followed. Null = standalone.</summary>
    public int?         ActivityLogId { get; set; }

    /// <summary>Internal audit timestamp. Never used for analytics or display.</summary>
    public DateTime CreatedAt    { get; set; }

    public ActivityLog? ActivityLog { get; set; }
}
