using Microsoft.EntityFrameworkCore;
using Momentum.Application.Interfaces;
using Momentum.Infrastructure.Data;
using Momentum.Shared;

namespace Momentum.Infrastructure.Services;

public class CategoryService(AppDbContext context) : ICategoryService
{
    public async Task<IEnumerable<CategoryDto>> GetAllAsync() =>
        await context.Dimensions
            .OrderBy(c => c.Id)
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name, ColorHex = c.ColorHex })
            .ToListAsync();
}
