using StoreManagementAPI.Models;
using StoreManagementAPI.Repositories;

namespace StoreManagementAPI.Services
{
    public interface IPromotionService
    {
        Task<Promotion?> ValidateAndGetPromotionAsync(string promoCode);
        Task<bool> UpdatePromotionStatusAsync(int promoId);
        Task UpdateAllPromotionStatusesAsync();
        Task<bool> IncrementUsageCountAsync(int promoId);
    }

    public class PromotionService : IPromotionService
    {
        private readonly IRepository<Promotion> _promotionRepository;

        public PromotionService(IRepository<Promotion> promotionRepository)
        {
            _promotionRepository = promotionRepository;
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
    }
}
