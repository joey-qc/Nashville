using System.ComponentModel.DataAnnotations;

namespace Momentum.Shared;

public class UpdateUserSettingsDto
{
    [Required, MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    public string Theme { get; set; } = "light";
}
