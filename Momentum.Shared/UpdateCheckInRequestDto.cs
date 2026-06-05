using System.ComponentModel.DataAnnotations;

namespace Momentum.Shared;

public class UpdateCheckInRequestDto
{
    [Required]
    public DateTime CheckedInAt { get; set; }

    [Required]
    [Range(-5, 5, ErrorMessage = "BodyScore must be between -5 and +5.")]
    public int BodyScore { get; set; }

    [Required]
    [Range(-5, 5, ErrorMessage = "EnergyScore must be between -5 and +5.")]
    public int EnergyScore { get; set; }

    [Required]
    [Range(-5, 5, ErrorMessage = "MoodScore must be between -5 and +5.")]
    public int MoodScore { get; set; }

    /// <summary>Pass null to detach from any activity log (make standalone).</summary>
    public int? ActivityLogId { get; set; }
}
