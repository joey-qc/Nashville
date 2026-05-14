namespace Momentum.Shared;

public class DeleteActivityResponseDto
{
    public int LogCount { get; set; }
    public string[] Options { get; set; } = ["archive", "cascade"];
}
