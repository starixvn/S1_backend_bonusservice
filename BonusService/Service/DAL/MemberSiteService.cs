using BonusService.Service.BLL;
using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using MemberSite.Service.Model;

namespace BonusService.Service.DAL
{
    public class MemberSiteService
    {
        private readonly MySqlClient _reportDbClient;
        
        public MemberSiteService(IServiceProvider provider)
        {
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }
        
        public IEnumerable<CustGiftList> GetCustDrawPrizeList(int siteId, int custId, int eventId)
        {
            return _reportDbClient.Query<CustGiftList>("Member_CustDrawPrizeList_Get",
                new { EventId = eventId, SiteId = siteId, CustId = custId });
        }

    }
}