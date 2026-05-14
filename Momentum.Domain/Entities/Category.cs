namespace Momentum.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;

    public ICollection<ActivityCategory> ActivityCategories { get; set; } = [];
}
