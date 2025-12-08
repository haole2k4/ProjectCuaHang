using Microsoft.EntityFrameworkCore;
using StoreManagementAPI.Data;
using StoreManagementAPI.Models;

namespace StoreManagementAPI.Services
{
    public class CategoryService
    {
        private readonly StoreDbContext _context;

        public CategoryService(StoreDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Category>> GetFilteredCategoriesAsync(string? search, string? status)
        {
            var query = _context.Categories.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(c => c.CategoryName.Contains(search));

            if (!string.IsNullOrEmpty(status) && status.ToLower() != "all")
                query = query.Where(c => c.Status.ToLower() == status.ToLower());

            return await query.ToListAsync();
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _context.Categories.FindAsync(id);
        }

        public async Task<Category> CreateAsync(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<Category> UpdateAsync(Category category)
        {
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
            return category;
        }
    }
}
