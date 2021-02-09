using BonusService.Service.Model;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace BonusService.Service.DAL
{
    public class ExpiredBonusService
    {
        private readonly IConfiguration configuration;
        private readonly WalletService walletService;
        private readonly WithdrawalLimitService withdrawalLimitService;

        public ExpiredBonusService(IConfiguration configuration, WalletService walletService, WithdrawalLimitService withdrawalLimitService)
        {
            this.configuration = configuration;
            this.walletService = walletService;
            this.withdrawalLimitService = withdrawalLimitService;
        }
        public IEnumerable<ExpiredBonus> GetBonusExpired(int limit)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<ExpiredBonus>("Service_BonusExpired_Get",new{ Limit = limit }, commandType: CommandType.StoredProcedure);
        }
        public int RefundExpiredBonus(ExpiredBonus expiredBonus)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
           using var trans = conn.BeginTransaction();
            int rtn = 0;
            if (expiredBonus.Refund > 0) //money
            {
                rtn = walletService.SetBalance(expiredBonus.CustId, expiredBonus.SiteId, expiredBonus.UserName, expiredBonus.Refund, 7, $"CancelBonus: {expiredBonus.Refund.ToString("#,##0.##")}", null, out _, out _, conn, trans);
                rtn = withdrawalLimitService.SetWithdrawalLimit(expiredBonus.CustId, expiredBonus.UserName, expiredBonus.SiteId, expiredBonus.Refund, "CancelBonus", null, out decimal afterWithdrawalLimit, conn, trans);
            }
            conn.Query<ExpiredBonus>("Service_BonusExpired_Set", new { BonusId = expiredBonus.BonusId }, commandType: CommandType.StoredProcedure);
           trans.Commit();
            return rtn;
        }
    }
}
