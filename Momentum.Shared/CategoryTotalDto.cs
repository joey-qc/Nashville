namespace Momentum.Shared;

public class CategoryTotalDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;
    public int Total { get; set; }
}
