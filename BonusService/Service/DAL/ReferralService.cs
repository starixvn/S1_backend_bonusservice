using BonusService.Service.Model;
using Dapper;
using BonusService.Service.DAL.Interface;
using BonusService.Service.Library;
using BonusService.Service.Model.enums;
using Microsoft.Extensions.Logging;

namespace BonusService.Service.DAL
{
    public class ReferralService : IReferralService
    {
        private readonly MySqlClient _reportDbClient;
        public ReferralService(IEnumerable<MySqlClient> dbClients)
        {
            _reportDbClient = dbClients.FirstOrDefault(a => a.DataBase == DataBase.Report);
        }
        public IEnumerable<ReferralInfo> GetReferralSettlement()
        {
            return _reportDbClient.Query<ReferralInfo>("Service_ReferralSettlement_Get");
        }

        public int GetRemainingCount(int agentId, int siteId, int currencyId)
        {
            var p = new DynamicParameters();
            p.Add("_AgentId", agentId);
            p.Add("_SiteId", siteId);
            p.Add("_CurrencyId", currencyId);
            return _reportDbClient.QueryFirst<int>("Service_ReferralRemainingCount_Get", p);
        }

        public IEnumerable<WorkerCustInfo> GetDepositReferralCustList(int siteId, int currencyId, DateTime specificDate, string currencyTimeZone)
        {
            var p = new DynamicParameters();
            p.Add("_SiteId", siteId);
            p.Add("_CurrencyId", currencyId);
            p.Add("_SpecificDate", specificDate);
            p.Add("_CurrencyTimeZone", currencyTimeZone);
            return _reportDbClient.Query<WorkerCustInfo>("Service_DepositReferralCustList_Get", p);
        }
    }
}
