using System.ComponentModel.DataAnnotations;

namespace Momentum.Shared;

public class UpdateActivityDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public int DefaultPoints { get; set; }

    [Required, MinLength(1)]
    public List<int> CategoryIds { get; set; } = [];
}
