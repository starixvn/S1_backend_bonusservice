using BonusService.Service.Model;
using BonusService.Service.Library;
using BonusService.Service.Model.enums;

namespace BonusService.Service.DAL
{
    public class CustInfoService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly Dictionary<int, bool> isConvert = new()
            {
                {51,false },{15,false }
            };

        public CustInfoService(IServiceProvider serviceProvider)
        {
            _mainDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }
        
        public bool IsConvertRequired(int CurrencyId)
        {
            return isConvert.ContainsKey(CurrencyId);
        }
        
        public Customer GetCustInfo(int custId)
        {
            return _mainDbClient.QueryFirst<Customer>("Customer_Get", new { CustId = custId });
        }
        
        public CustInfoProfile GetCustInfoProfile(int custId, int siteId)
        {
            return _reportDbClient.QueryFirst<CustInfoProfile>("Member_CustProfileV2_Get", new { CustId = custId, SiteId = siteId });
        }
    }
}
