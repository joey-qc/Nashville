using System.ComponentModel.DataAnnotations;

namespace Momentum.Shared;

public class UpdateActivityLogDto
{
    [Required]
    public int ActivityId { get; set; }

    [Required]
    public DateTime LoggedAt { get; set; }

    [Required]
    public int PointsRecorded { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public List<int>? DimensionIds { get; set; }
}
