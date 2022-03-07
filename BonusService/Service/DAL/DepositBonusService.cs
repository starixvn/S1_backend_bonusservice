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
    public class DepositBonusService
    {
        private readonly IConfiguration configuration;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly string _connectionString;
        private readonly Common common;
        private readonly WalletService walletService;
        private readonly WithdrawalLimitService withdrawalLimitService;
        private readonly BonusWalletService _bonusWalletService;

        public DepositBonusService(IServiceProvider serviceProvider)
        {
            configuration = serviceProvider.GetRequiredService<IConfiguration>();
            _connectionString = configuration.GetConnectionString("ConnectionString");
            common = serviceProvider.GetRequiredService<Common>();
            walletService = serviceProvider.GetRequiredService<WalletService>();
            withdrawalLimitService = serviceProvider.GetRequiredService<WithdrawalLimitService>();
            _mainDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _bonusWalletService = serviceProvider.GetRequiredService<BonusWalletService>();
        }
        public IEnumerable<DepositBonus> GetDepositBonusQueue()
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<DepositBonus>("Service_DepositBonusQueue_Get", commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<DepositBonusCustList> GetDepositBonusQueueCustList()
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<DepositBonusCustList>("Service_DepositBonusQueueCustList_Get", commandType: CommandType.StoredProcedure);
        }

        public Promotion GetMemberPromotion(DynamicParameters parameters)
        {
            return _reportDbClient.QueryFirst<Promotion>("Service_Promotion_Get", parameters);
        }

        //public bool RewardBonus(int bonusId, int promoCategoryId, int tpId, int custId, int siteId, string userName, decimal bonusAmount, decimal lockedAmount, decimal rollover, long expiryTime, int type, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        //{
        //    var p = new DynamicParameters(new { BonusId = bonusId, PromoCategoryId = promoCategoryId, TpId = tpId, CustId = custId, SiteId = siteId, UserName = userName, BonusAmount = bonusAmount, LockedAmount = lockedAmount, Rollover = rollover, ExpiryTime = expiryTime, Type = type,
        //        Memo,
        //        RefTransId });
        //    return conn.Execute("Common_Bonus_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure) > 0;
        //}
        public void CheckDepositBonusExpired()
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Execute("Service_DepositBonusRemoveExpired_Set", commandType: CommandType.StoredProcedure);
        }
        public int SetDepositBonusStatus(string transId, int status)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_DepositBonusStatus_Set", new { TransId = transId, Status = status }, commandType: CommandType.StoredProcedure);
        }
    }
}
