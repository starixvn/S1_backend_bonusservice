using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;

using Dapper;

namespace BonusService.Service.DAL
{
    public class PromotionCheckService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;

        public PromotionCheckService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }


        public IEnumerable<PromotionCheck> GetPromotionCheckList(int limit)
        {
            return _reportDbClient.QueryUseCommandDefinition<PromotionCheck>("Service_Promotion_Check", new DynamicParameters(new { Limit = limit }));
        }

        public int SetPromotionClose(int SiteId, string BonusCode)
        {
            return _mainDbClient.Execute("Service_Promotion_Close", new { SiteId, BonusCode, LastUpdateUser = "ServiceAutoCheck" });
        }

        public int SetPromotionEndTime(int SiteId, string BonusCode, long EndTime)
        {
            return _mainDbClient.Execute("Service_Promotion_EndTime_Set", new { SiteId, BonusCode, EndTime, LastUpdateUser = "ServiceAutoCheck" });
        }
    }
}
