namespace Momentum.Shared;

public class CheckInDto
{
    public int      Id            { get; set; }
    public string   UserId        { get; set; } = string.Empty;
    public DateTime CheckedInAt   { get; set; }
    public int      BodyScore     { get; set; }
    public int      EnergyScore   { get; set; }
    public int      MoodScore     { get; set; }
    public int?     ActivityLogId { get; set; }

    /// <summary>Name of the linked activity (when ActivityLogId is set); null for standalone check-ins. Display-only.</summary>
    public string?  ActivityName  { get; set; }

    public DateTime CreatedAt     { get; set; }
}
