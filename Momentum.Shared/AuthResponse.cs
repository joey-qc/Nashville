namespace Momentum.Shared;

public class AuthResponse
{
    public bool Succeeded { get; set; }
    public string? Token { get; set; }
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public IEnumerable<string> Errors { get; set; } = [];
}
