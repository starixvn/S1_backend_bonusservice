using BonusService.Service.DAL.Interface;
using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;

namespace BonusService.Service.DAL
{
    public class CommonService : ICommonService
    {
        private readonly MySqlClient _reportDbClient;

        public CommonService(IServiceProvider provider)
        {
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public IEnumerable<CurrencyTimeZoneDto> GetCurrencyTimeZones()
        {
            return _reportDbClient.Query<CurrencyTimeZoneDto>("Service_CurrencyTimeZone_Get", null);
        }
    }
}
