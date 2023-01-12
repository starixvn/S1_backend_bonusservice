using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;

namespace BonusService.Service.DAL
{
    public class RebateService
    {
        private readonly MySqlClient _reportDbClient;
        
        public RebateService(IServiceProvider serviceProvider)
        {
            _reportDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public IEnumerable<TpCategory> GetTpCategoryList()
        {
            return _reportDbClient.Query<TpCategory>("Common_TpCategory_Get");
        }
    }
}
