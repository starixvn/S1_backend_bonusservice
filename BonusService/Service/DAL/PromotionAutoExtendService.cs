using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;

namespace BonusService.Service.DAL
{
    public class PromotionAutoExtendService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;

        public PromotionAutoExtendService(IServiceProvider serviceProvider)
        {
            _mainDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public IEnumerable<PromotionAutoExtend> GetAutoExtendPromotionExpiredList(string specificDate, int limit)
        {
            return _reportDbClient.QueryUseCommandDefinition<PromotionAutoExtend>("Service_PromotionAutoExtend_Get_V2",
                new { SpecificDate = specificDate, Limit = limit });
        }

        public int SetPromotionExtend(PromotionAutoExtend PromotionExtend)
        {
            return _mainDbClient.Execute("Service_PromotionAutoExtend_Set", new { PromotionExtend.BonusCode, PromotionExtend.SiteId, PeriodTo = PromotionExtend.ExtendTime, LastUpdateUser = "ServiceAuto" });
        }
    }
}
