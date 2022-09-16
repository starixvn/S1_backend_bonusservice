using BonusService.Service.Library;
using BonusService.Service.Model;
using Dapper;
using MySql.Data.MySqlClient;
using System.Data;

namespace BonusService.Service.DAL
{
    public class RebateService
    {
        private readonly IConfiguration configuration;

        public RebateService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public IEnumerable<TpCategory> GetTpCategoryList()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<TpCategory>("Common_TpCategory_Get", commandType: CommandType.StoredProcedure);
        }
    }
}
