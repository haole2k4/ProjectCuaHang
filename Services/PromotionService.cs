using StoreManagementAPI.Models;
using StoreManagementAPI.Repositories;
using StoreManagementAPI.DTOs;
using StoreManagementAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace StoreManagementAPI.Services
{
    public interface IPromotionService
    {
        Task<Promotion?> ValidateAndGetPromotionAsync(string promoCode);
        Task<bool> UpdatePromotionStatusAsync(int promoId);
        Task UpdateAllPromotionStatusesAsync();
        Task<bool> IncrementUsageCountAsync(int promoId);
        Task<PromotionSearchResultDto> SearchPromotionsAdvancedAsync(PromotionSearchDto searchDto);
        Task<Promotion> CreatePromotionAsync(CreatePromotionDto dto);
        Task<Promotion> UpdatePromotionAsync(int promoId, CreatePromotionDto dto);
        Task<bool> DeletePromotionAsync(int promoId);
        Task<List<PromotionDisplayDto>> GetValidPromotionsForOrderAsync(decimal orderAmount);
    }

    public class PromotionService : IPromotionService
    {
        private readonly IRepository<Promotion> _promotionRepository;
        private readonly StoreDbContext _context;

        public PromotionService(IRepository<Promotion> promotionRepository, StoreDbContext context)
        {
            _promotionRepository = promotionRepository;
            _context = context;
        }

        public async Task<Promotion?> ValidateAndGetPromotionAsync(string promoCode)
        {
            Console.WriteLine($"[PromotionService] Validating promo code: '{promoCode}'");

            // Use case-insensitive search by converting both to uppercase for comparison
            var promotions = await _promotionRepository.FindAsync(p => p.PromoCode.ToUpper() == promoCode.ToUpper());
            var promotion = promotions.FirstOrDefault();

            if (promotion == null)
            {
                Console.WriteLine($"[PromotionService] Promotion not found for code: '{promoCode}'");
                return null;
            }

            Console.WriteLine($"[PromotionService] Found promotion: {promotion.PromoCode}, Status: {promotion.Status}");
            Console.WriteLine($"[PromotionService] Before update - StartDate: {promotion.StartDate}, EndDate: {promotion.EndDate}");

            await UpdatePromotionStatusAsync(promotion.PromoId);
            promotion = await _promotionRepository.GetByIdAsync(promotion.PromoId);

            if (promotion == null || promotion.Status != "active")
            {
                Console.WriteLine($"[PromotionService] Promotion validation failed - Status: {promotion?.Status ?? "null"}");
                return null;
            }

            var today = DateTime.Now.Date;
            var startDate = promotion.StartDate.Date;
            var endDate = promotion.EndDate.Date;

            Console.WriteLine($"[PromotionService] Date comparison - Today: {today}, Start: {startDate}, End: {endDate}");

            if (today < startDate || today > endDate)
            {
                Console.WriteLine($"[PromotionService] Date validation failed");
                return null;
            }

            if (promotion.UsedCount >= promotion.UsageLimit && promotion.UsageLimit > 0)
            {
                Console.WriteLine($"[PromotionService] Usage limit reached: {promotion.UsedCount}/{promotion.UsageLimit}");
                return null;
            }

            Console.WriteLine($"[PromotionService] Promotion validated successfully!");
            return promotion;
        }

        public async Task<bool> UpdatePromotionStatusAsync(int promoId)
        {
            var promotion = await _promotionRepository.GetByIdAsync(promoId);
            if (promotion == null)
                return false;

            var today = DateTime.Now.Date;
            var startDate = promotion.StartDate.Date;
            var endDate = promotion.EndDate.Date;
            string newStatus = promotion.Status;

            Console.WriteLine($"[UpdatePromotionStatus] PromoId: {promoId}");
            Console.WriteLine($"[UpdatePromotionStatus] Current Status: {promotion.Status}");
            Console.WriteLine($"[UpdatePromotionStatus] Today: {today:yyyy-MM-dd}, Start: {startDate:yyyy-MM-dd}, End: {endDate:yyyy-MM-dd}");
            Console.WriteLine($"[UpdatePromotionStatus] UsedCount: {promotion.UsedCount}, UsageLimit: {promotion.UsageLimit}");

            if (today < startDate)
            {
                newStatus = "inactive";
                Console.WriteLine($"[UpdatePromotionStatus] Setting inactive: today < startDate");
            }
            else if (today > endDate)
            {
                newStatus = "inactive";
                Console.WriteLine($"[UpdatePromotionStatus] Setting inactive: today > endDate");
            }
            else if (promotion.UsedCount >= promotion.UsageLimit && promotion.UsageLimit > 0)
            {
                newStatus = "inactive";
                Console.WriteLine($"[UpdatePromotionStatus] Setting inactive: usage limit reached");
            }
            else if (today >= startDate && today <= endDate && (promotion.UsedCount < promotion.UsageLimit || promotion.UsageLimit == 0))
            {
                newStatus = "active";
                Console.WriteLine($"[UpdatePromotionStatus] Setting active: within valid date range");
            }

            Console.WriteLine($"[UpdatePromotionStatus] New Status: {newStatus}");

            if (newStatus != promotion.Status)
            {
                promotion.Status = newStatus;
                await _promotionRepository.UpdateAsync(promotion);
                Console.WriteLine($"[UpdatePromotionStatus] Status updated from {promotion.Status} to {newStatus}");
                return true;
            }

            return false;
        }

        public async Task UpdateAllPromotionStatusesAsync()
        {
            var allPromotions = await _promotionRepository.GetAllAsync();

            foreach (var promotion in allPromotions)
            {
                await UpdatePromotionStatusAsync(promotion.PromoId);
            }
        }

        public async Task<bool> IncrementUsageCountAsync(int promoId)
        {
            var promotion = await _promotionRepository.GetByIdAsync(promoId);
            if (promotion == null)
                return false;

            promotion.UsedCount++;

            if (promotion.UsedCount >= promotion.UsageLimit && promotion.UsageLimit > 0)
            {
                promotion.Status = "inactive";
            }

            await _promotionRepository.UpdateAsync(promotion);
            return true;
        }

        public async Task<Promotion> CreatePromotionAsync(CreatePromotionDto dto)
        {
            var promotion = new Promotion
            {
                PromoCode = dto.PromoCode,
                Description = dto.Description,
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                MinOrderAmount = dto.MinOrderAmount,
                UsageLimit = dto.UsageLimit,
                UsedCount = 0,
                Status = dto.Status,
                ApplyType = dto.ApplyType
            };

            var result = await _promotionRepository.AddAsync(promotion);
            return result;
        }

        public async Task<Promotion> UpdatePromotionAsync(int promoId, CreatePromotionDto dto)
        {
            var promotion = await _promotionRepository.GetByIdAsync(promoId);
            if (promotion == null)
                throw new Exception("Promotion not found");

            promotion.PromoCode = dto.PromoCode;
            promotion.Description = dto.Description;
            promotion.DiscountType = dto.DiscountType;
            promotion.DiscountValue = dto.DiscountValue;
            promotion.StartDate = dto.StartDate;
            promotion.EndDate = dto.EndDate;
            promotion.MinOrderAmount = dto.MinOrderAmount;
            promotion.UsageLimit = dto.UsageLimit;
            promotion.Status = dto.Status;
            promotion.ApplyType = dto.ApplyType;

            await _promotionRepository.UpdateAsync(promotion);
            return promotion;
        }

        public async Task<bool> DeletePromotionAsync(int promoId)
        {
            var promotion = await _promotionRepository.GetByIdAsync(promoId);
            if (promotion == null)
                return false;

            promotion.Status = "deleted";
            await _promotionRepository.UpdateAsync(promotion);
            return true;
        }

        public async Task<PromotionSearchResultDto> SearchPromotionsAdvancedAsync(PromotionSearchDto searchDto)
        {
            // Validate and sanitize pagination parameters
            var pageNumber = searchDto.PageNumber < 1 ? 1 : searchDto.PageNumber;
            var pageSize = searchDto.PageSize < 1 ? 20 : searchDto.PageSize > 100 ? 100 : searchDto.PageSize;

            // Validate and swap date range if needed
            if (searchDto.StartDate.HasValue && searchDto.EndDate.HasValue && searchDto.StartDate.Value > searchDto.EndDate.Value)
            {
                var temp = searchDto.StartDate;
                searchDto.StartDate = searchDto.EndDate;
                searchDto.EndDate = temp;
            }

            // Start with base query
            var query = _context.Promotions
                .Include(p => p.PromotionProducts)
                    .ThenInclude(pp => pp.Product)
                .AsQueryable();

            // Apply filters
            // 1. Filter by search term (promo code or description)
            if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
            {
                var searchTerm = searchDto.SearchTerm.ToLower().Trim();
                query = query.Where(p =>
                    p.PromoCode.ToLower().Contains(searchTerm) ||
                    (p.Description != null && p.Description.ToLower().Contains(searchTerm)));
            }

            // 2. Filter by status (only if Status is not null or empty)
            if (!string.IsNullOrWhiteSpace(searchDto.Status))
            {
                var status = searchDto.Status.ToLower().Trim();
                query = query.Where(p => p.Status != null && p.Status.ToLower() == status);
            }

            // 3. Filter by discount type
            if (!string.IsNullOrWhiteSpace(searchDto.DiscountType))
            {
                var discountType = searchDto.DiscountType.ToLower().Trim();
                query = query.Where(p => p.DiscountType != null && p.DiscountType.ToLower() == discountType);
            }

            // 4. Filter by apply type
            if (!string.IsNullOrWhiteSpace(searchDto.ApplyType))
            {
                var applyType = searchDto.ApplyType.ToLower().Trim();
                query = query.Where(p => p.ApplyType != null && p.ApplyType.ToLower() == applyType);
            }

            // 5. Filter by date range
            if (searchDto.StartDate.HasValue)
            {
                query = query.Where(p => p.EndDate >= searchDto.StartDate.Value);
            }

            if (searchDto.EndDate.HasValue)
            {
                query = query.Where(p => p.StartDate <= searchDto.EndDate.Value);
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply sorting
            var sortBy = searchDto.SortBy?.ToLower() ?? "startdate";
            var sortDirection = searchDto.SortDirection?.ToLower() ?? "desc";

            query = sortBy switch
            {
                "promocode" => sortDirection == "desc"
                    ? query.OrderByDescending(p => p.PromoCode)
                    : query.OrderBy(p => p.PromoCode),
                "discountvalue" => sortDirection == "desc"
                    ? query.OrderByDescending(p => p.DiscountValue)
                    : query.OrderBy(p => p.DiscountValue),
                "enddate" => sortDirection == "desc"
                    ? query.OrderByDescending(p => p.EndDate)
                    : query.OrderBy(p => p.EndDate),
                _ => sortDirection == "desc"
                    ? query.OrderByDescending(p => p.StartDate)
                    : query.OrderBy(p => p.StartDate)
            };

            // Apply pagination
            var promotions = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map to DTOs
            var promotionDtos = promotions.Select(p => new PromotionDto
            {
                PromoId = p.PromoId,
                PromoCode = p.PromoCode,
                Description = p.Description,
                DiscountType = p.DiscountType,
                DiscountValue = p.DiscountValue,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                MinOrderAmount = p.MinOrderAmount,
                UsageLimit = p.UsageLimit,
                UsedCount = p.UsedCount,
                Status = p.Status,
                ApplyType = p.ApplyType,
                Products = p.PromotionProducts.Select(pp => new ProductSimpleDto
                {
                    ProductId = pp.ProductId,
                    ProductName = pp.Product?.ProductName ?? "Unknown",
                    Price = pp.Product?.Price ?? 0
                }).ToList()
            }).ToList();

            // Calculate pagination metadata
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return new PromotionSearchResultDto
            {
                Promotions = promotionDtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasPreviousPage = pageNumber > 1,
                HasNextPage = pageNumber < totalPages
            };
        }

        public async Task<List<PromotionDisplayDto>> GetValidPromotionsForOrderAsync(decimal orderAmount)
        {
            var today = DateTime.Now.Date;
            
            var validPromotions = await _context.Promotions
                .Where(p => p.Status == "active" && 
                           p.StartDate.Date <= today && 
                           p.EndDate.Date >= today &&
                           (p.UsageLimit == 0 || p.UsedCount < p.UsageLimit) &&
                           p.MinOrderAmount <= orderAmount &&
                           p.ApplyType == "order")
                .ToListAsync();

            var promotionDisplayList = validPromotions.Select(p => 
            {
                decimal discountAmount = 0;
                string displayText = "";

                if (p.DiscountType == "percent")
                {
                    discountAmount = orderAmount * (p.DiscountValue / 100);
                    displayText = $"{p.PromoCode} - Giảm {p.DiscountValue}% (Tiết kiệm: ${discountAmount:N2})";
                }
                else // fixed
                {
                    discountAmount = Math.Min(p.DiscountValue, orderAmount);
                    displayText = $"{p.PromoCode} - Giảm ${p.DiscountValue:N2} (Tiết kiệm: ${discountAmount:N2})";
                }

                if (!string.IsNullOrEmpty(p.Description))
                {
                    displayText += $" - {p.Description}";
                }

                return new PromotionDisplayDto
                {
                    PromoId = p.PromoId,
                    PromoCode = p.PromoCode,
                    DisplayText = displayText,
                    DiscountAmount = discountAmount,
                    DiscountType = p.DiscountType,
                    DiscountValue = p.DiscountValue,
                    Description = p.Description
                };
            }).OrderByDescending(p => p.DiscountAmount).ToList();

            return promotionDisplayList;
        }
    }
}
