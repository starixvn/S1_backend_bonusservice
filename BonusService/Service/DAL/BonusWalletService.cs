using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;

namespace BonusService.Service.DAL
{
    public class BonusWalletService
    {
        
        private readonly IConfiguration configuration;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly int typePosition = 1000;

        public BonusWalletService(IServiceProvider provider)
        {
            configuration = provider.GetRequiredService<IConfiguration>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public bool RewardBonus(int bonusId, int promoCategoryId, int tpId, int custId, int siteId, string userName, decimal bonusAmount, decimal lockedAmount, decimal rollover, long expiryTime, int type, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new { BonusId = bonusId, PromoCategoryId = promoCategoryId, TpId = tpId, CustId = custId, SiteId = siteId, UserName = userName, BonusAmount = bonusAmount, LockedAmount = lockedAmount, Rollover = rollover, ExpiryTime = expiryTime, Type = type,
                Memo,
                RefTransId });
            return _mainDbClient.Execute("Common_Bonus_Ins", p, conn, trans) > 0;
        }
        public bool RewardRebate(string bonusCode, int rebateType, int custId, string userName, int siteId, int currencyId, MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new { BonusCode = bonusCode, RebateType = rebateType, CustId = custId, UserName = userName, SiteId = siteId, CurrencyId = currencyId });
            return _mainDbClient.Execute("Common_Rebate_Ins", p, conn, trans) > 0;
        }

        public int RewardCashBack(CustCashBackInput custCashBackInput, MySqlConnection connection, MySqlTransaction trans)
        {
            var parameter = new DynamicParameters(new
            {
                custCashBackInput.CustId,
                custCashBackInput.BonusCode,
                custCashBackInput.PromoCategoryId,
                custCashBackInput.CalculateDate
            });
            return _mainDbClient.Execute("Member_CustCashBack_Set", parameter, connection, trans);
        }

        public IEnumerable<BonusWallet> GetCustBonusWallet(int CustId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<BonusWallet>("Member_CustBonus_Get", new { CustId }, commandType: CommandType.StoredProcedure);
        }

        public BonusWallet CheckBonusWallet(int CustId, int TpId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<BonusWallet>("Member_CustBonus_Check", new { CustId, TpId }, commandType: CommandType.StoredProcedure);
        }
        public BonusWallet CheckBonusWallet(int CustId, string bonusCode)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<BonusWallet>("Member_CustBonusByCode_Get", new { CustId, BonusCode = bonusCode }, commandType: CommandType.StoredProcedure);
        }

        public BonusWallet CustBonusInfo(int CustId, string BonusCode, int TpId, int PromoCategoryId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<BonusWallet>("Member_CustBonus_GetById", new { CustId, BonusCode, TpId, PromoCategoryId }, commandType: CommandType.StoredProcedure);
        }
    }
}
