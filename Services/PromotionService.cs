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
        Task<Promotion> CreatePromotionAsync(CreatePromotionDto dto);
        Task<Promotion> UpdatePromotionAsync(int promoId, CreatePromotionDto dto);
        Task<bool> DeletePromotionAsync(int promoId);
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
    }
}
