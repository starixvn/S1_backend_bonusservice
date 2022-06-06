using BonusService.Service.Model;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Data;

namespace BonusService.Service.DAL
{
    public class PromotionAutoExtendService
    {
        private readonly IConfiguration configuration;

        public PromotionAutoExtendService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public IEnumerable<PromotionAutoExtend> GetAutoExtendPromotionExpiredList( string specificDate = null)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<PromotionAutoExtend>("Service_PromotionAutoExtend_Get", new { SpecificDate = specificDate }, commandType: CommandType.StoredProcedure);
        }
        public int SetPromotionExtend(PromotionAutoExtend PromotionExtend)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_PromotionAutoExtend_Set", new { PromotionExtend.BonusCode, PromotionExtend.SiteId, PeriodTo = PromotionExtend.ExtendTime, LastUpdateUser = "ServiceAuto" }, commandType: CommandType.StoredProcedure);
        }
    }
}
