using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Data;
using BonusService.Service.Model;

namespace BonusService.Service.DAL
{
    public class BonusCashBackService
    {
        private IConfiguration configuration;

        public BonusCashBackService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public IEnumerable<BonusCashBack> GetBonusCashBackInfo()
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.Query<BonusCashBack>("Service_BonusCashBack_Get", commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<BonusCashBackGames> GetBonusCashBackGamesInfo(int CashbackId, int SiteId)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.Query<BonusCashBackGames>("Service_BonusCashBackGames_Get", new { CashbackId, SiteId }, commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<BonusCashBackMinMax> GetBonusCashBackMinMaxInfo(int CashbackId, int SiteId)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.Query<BonusCashBackMinMax>("Service_BonusCashBackMinMax_Get", new { CashbackId, SiteId }, commandType: CommandType.StoredProcedure);
            }
        }
    }
}
