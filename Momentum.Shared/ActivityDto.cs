namespace Momentum.Shared;

public class ActivityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DefaultPoints { get; set; }
    public bool IsArchived { get; set; }
    public List<CategoryDto> Categories { get; set; } = [];
}
