using BonusService.Service.Model;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using BonusService.Service.Library;
using BonusService.Service.Model.enums;
using Microsoft.Extensions.DependencyInjection;

namespace BonusService.Service.DAL
{
    public class ExpiredBonusService
    {
        private readonly IConfiguration _configuration;
        private readonly WalletService _walletService;
        private readonly WithdrawalLimitService _withdrawalLimitService;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;

        public ExpiredBonusService(IServiceProvider provider)
        {
            _configuration = provider.GetRequiredService<IConfiguration>();
            _walletService = provider.GetRequiredService<WalletService>();
            _withdrawalLimitService = provider.GetRequiredService<WithdrawalLimitService>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }
        public IEnumerable<ExpiredBonus> GetBonusExpired(int limit)
        {
            using MySqlConnection conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionString"));
            return conn.Query<ExpiredBonus>("Service_BonusExpired_Get", new { Limit = limit }, commandType: CommandType.StoredProcedure);
        }
        public int RefundExpiredBonus(ExpiredBonus expiredBonus)
        {
            using MySqlConnection conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            int rtn = 0;
            if (expiredBonus.Refund > 0) //money
            {
                rtn = _walletService.SetBalance(expiredBonus.CustId, expiredBonus.SiteId, expiredBonus.UserName, expiredBonus.Refund, 7, $"CancelBonus: {expiredBonus.Refund:#,##0.##}", expiredBonus.BonusId.ToString(), out _, out _, conn, trans);
                rtn = _withdrawalLimitService.SetWithdrawalLimit(expiredBonus.CustId, expiredBonus.UserName, expiredBonus.SiteId, expiredBonus.Refund, "CancelBonus", expiredBonus.BonusId.ToString(), out decimal afterWithdrawalLimit, conn, trans);
            }

            rtn = conn.Execute("Service_BonusExpired_Set", new { BonusId = expiredBonus.BonusId, CustId = expiredBonus.CustId, UserName = expiredBonus.UserName, SiteId = expiredBonus.SiteId, TYPE = 7, PromoCategoryId = expiredBonus.PromoCategoryId, Bonus = expiredBonus.Bonus }, commandType: CommandType.StoredProcedure);

            if (rtn > 0)
            {
                trans.Commit();
            }

            return rtn;
        }
    }
}
