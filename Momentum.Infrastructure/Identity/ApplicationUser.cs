using Microsoft.AspNetCore.Identity;

namespace Momentum.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public string Theme { get; set; } = "light";
}
