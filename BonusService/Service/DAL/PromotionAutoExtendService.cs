using BonusService.Service.Model;
using System.Data;
using BonusService.Service.Library;
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
        
        public IEnumerable<PromotionAutoExtend> GetAutoExtendPromotionExpiredList( string specificDate = null)
        {
            return _reportDbClient.Query<PromotionAutoExtend>("Service_PromotionAutoExtend_Get", new { SpecificDate = specificDate }, commandType: CommandType.StoredProcedure);
        }
        
        public int SetPromotionExtend(PromotionAutoExtend PromotionExtend)
        {
            return _mainDbClient.Execute("Service_PromotionAutoExtend_Set", new { PromotionExtend.BonusCode, PromotionExtend.SiteId, PeriodTo = PromotionExtend.ExtendTime, LastUpdateUser = "ServiceAuto" });
        }
    }
}
