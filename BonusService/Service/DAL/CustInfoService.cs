using BonusService.Service.Model;
using Dapper;
using MySql.Data.MySqlClient;
using System.Data;

namespace BonusService.Service.DAL
{
    public class CustInfoService
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<CustInfoService> _logger;
        private readonly Dictionary<int, bool> isConvert = new Dictionary<int, bool>()
            {
                {51,false },{15,false }
            };

        public CustInfoService(IConfiguration configuration, ILogger<CustInfoService> logger)
        {
            this.configuration = configuration;
            _logger = logger;
        }
        public bool IsConvertRequired(int CurrencyId)
        {
            return isConvert.ContainsKey(CurrencyId);
        }
        public Customer GetCustInfo(int custId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<Customer>("Customer_Get", new { CustId = custId }, commandType: CommandType.StoredProcedure);
        }
        public CustInfoProfile GetCustInfoProfile(int custId, int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<CustInfoProfile>("Member_CustProfileV2_Get", new { CustId = custId, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
    }
}
