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
            var promotions = await _promotionRepository.FindAsync(p => p.PromoCode == promoCode);
            var promotion = promotions.FirstOrDefault();

            if (promotion == null)
                return null;

            await UpdatePromotionStatusAsync(promotion.PromoId);
            promotion = await _promotionRepository.GetByIdAsync(promotion.PromoId);

            if (promotion == null || promotion.Status != "active")
                return null;

            var today = DateTime.Now.Date;
            var startDate = promotion.StartDate.Date;
            var endDate = promotion.EndDate.Date;

            if (today < startDate || today > endDate)
                return null;

            if (promotion.UsedCount >= promotion.UsageLimit && promotion.UsageLimit > 0)
                return null;

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

            if (today < startDate)
            {
                newStatus = "inactive";
            }
            else if (today > endDate)
            {
                newStatus = "inactive";
            }
            else if (promotion.UsedCount >= promotion.UsageLimit && promotion.UsageLimit > 0)
            {
                newStatus = "inactive";
            }
            else if (today >= startDate && today <= endDate && (promotion.UsedCount < promotion.UsageLimit || promotion.UsageLimit == 0))
            {
                newStatus = "active";
            }

            if (newStatus != promotion.Status)
            {
                promotion.Status = newStatus;
                await _promotionRepository.UpdateAsync(promotion);
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
