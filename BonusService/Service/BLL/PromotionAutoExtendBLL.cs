using BonusService.Service.DAL;
using BonusService.Service.Model;

namespace BonusService.Service.BLL
{
    public class PromotionAutoExtendBLL
    {
        private readonly PromotionAutoExtendService promotionAutoExtendService;

        public PromotionAutoExtendBLL(PromotionAutoExtendService promotionAutoExtendService)
        {
            this.promotionAutoExtendService = promotionAutoExtendService;
        }
        public IEnumerable<PromotionAutoExtend> GetAutoExtendPromotionExpiredList(string specificDate = null)
        {
            return promotionAutoExtendService.GetAutoExtendPromotionExpiredList(specificDate);
        }
        public int SetPromotionExtend(PromotionAutoExtend PromotionExtend)
        {
            return promotionAutoExtendService.SetPromotionExtend(PromotionExtend);
        }
    }
}
