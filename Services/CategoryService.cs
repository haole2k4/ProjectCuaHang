using Microsoft.EntityFrameworkCore;
using StoreManagementAPI.Data;
using StoreManagementAPI.Models;
using StoreManagementAPI.DTOs;
using StoreManagementAPI.Repositories;

namespace StoreManagementAPI.Services
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
        Task<CategoryDto?> GetCategoryByIdAsync(int id);
        Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto);
        Task<CategoryDto?> UpdateCategoryAsync(int id, UpdateCategoryDto dto);
        Task<CategoryDeleteResponseDto> DeleteCategoryAsync(int id);
    }

    public class CategoryService : ICategoryService
    {
        private readonly StoreDbContext _context;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CategoryService(
            StoreDbContext context,
            IRepository<Category> categoryRepository,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _categoryRepository = categoryRepository;
            _auditLogService = auditLogService;
            _httpContextAccessor = httpContextAccessor;
        }

        private (string? userId, string? username) GetAuditInfo()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return (null, "admin");

            var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var usernameClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

            var username = !string.IsNullOrEmpty(usernameClaim) ? usernameClaim : "admin";

            return (userIdClaim, username);
        }

        public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
        {
            var categories = await _context.Categories
                .Include(c => c.Products)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            return categories.Select(c => new CategoryDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                Status = c.Status,
                ProductCount = c.Products.Count(p => p.Status == "active")
            });
        }

        public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null) return null;

            return new CategoryDto
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                Status = category.Status,
                ProductCount = category.Products.Count(p => p.Status == "active")
            };
        }

        public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var category = new Category
                {
                    CategoryName = dto.CategoryName,
                    Status = "active"
                };

                var createdCategory = await _categoryRepository.AddAsync(category);

                var (userId, username) = GetAuditInfo();
                await _auditLogService.LogActionAsync(
                    action: "CREATE",
                    entityType: "Category",
                    entityId: createdCategory.CategoryId,
                    entityName: createdCategory.CategoryName,
                    oldValues: null,
                    newValues: new
                    {
                        CategoryId = createdCategory.CategoryId,
                        CategoryName = createdCategory.CategoryName,
                        Status = createdCategory.Status
                    },
                    changesSummary: $"T?o danh m?c m?i: {createdCategory.CategoryName}",
                    userId: userId,
                    username: username
                );

                await transaction.CommitAsync();

                return await GetCategoryByIdAsync(createdCategory.CategoryId) ?? new CategoryDto();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<CategoryDto?> UpdateCategoryAsync(int id, UpdateCategoryDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var category = await _categoryRepository.GetByIdAsync(id);
                if (category == null) return null;

                var oldValues = new
                {
                    CategoryId = category.CategoryId,
                    CategoryName = category.CategoryName,
                    Status = category.Status
                };

                var changes = new List<string>();

                if (!string.IsNullOrEmpty(dto.CategoryName) && category.CategoryName != dto.CategoryName)
                {
                    changes.Add($"Tên: '{category.CategoryName}' ? '{dto.CategoryName}'");
                    category.CategoryName = dto.CategoryName;
                }

                if (!string.IsNullOrEmpty(dto.Status) && category.Status != dto.Status)
                {
                    changes.Add($"Tr?ng thái: {category.Status} ? {dto.Status}");
                    category.Status = dto.Status;
                }

                await _categoryRepository.UpdateAsync(category);

                if (changes.Any())
                {
                    var (userId, username) = GetAuditInfo();
                    await _auditLogService.LogActionAsync(
                        action: "UPDATE",
                        entityType: "Category",
                        entityId: category.CategoryId,
                        entityName: category.CategoryName,
                        oldValues: oldValues,
                        newValues: new
                        {
                            CategoryId = category.CategoryId,
                            CategoryName = category.CategoryName,
                            Status = category.Status
                        },
                        changesSummary: $"C?p nh?t danh m?c '{category.CategoryName}': {string.Join(", ", changes)}",
                        userId: userId,
                        username: username
                    );
                }

                await transaction.CommitAsync();
                return await GetCategoryByIdAsync(id);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<CategoryDeleteResponseDto> DeleteCategoryAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.CategoryId == id);

                if (category == null)
                {
                    return new CategoryDeleteResponseDto
                    {
                        Success = false,
                        SoftDeleted = false,
                        Message = "Không tìm th?y danh m?c",
                        CategoryId = id
                    };
                }

                if (category.Products != null && category.Products.Any())
                {
                    return new CategoryDeleteResponseDto
                    {
                        Success = false,
                        SoftDeleted = false,
                        Message = $"Không th? xóa danh m?c '{category.CategoryName}' vì ?ang ch?a {category.Products.Count} s?n ph?m.",
                        CategoryId = category.CategoryId,
                        ProductCount = category.Products.Count
                    };
                }

                var oldStatus = category.Status;
                category.Status = "inactive";
                await _categoryRepository.UpdateAsync(category);

                var (userId, username) = GetAuditInfo();
                await _auditLogService.LogActionAsync(
                    action: "SOFT_DELETE",
                    entityType: "Category",
                    entityId: category.CategoryId,
                    entityName: category.CategoryName,
                    oldValues: new { Status = oldStatus },
                    newValues: new { Status = "inactive" },
                    changesSummary: $"?n danh m?c '{category.CategoryName}' (không có s?n ph?m)",
                    userId: userId,
                    username: username
                );

                await transaction.CommitAsync();

                return new CategoryDeleteResponseDto
                {
                    Success = true,
                    SoftDeleted = true,
                    Message = $"?ã ?n danh m?c '{category.CategoryName}' thành công",
                    CategoryId = category.CategoryId
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
