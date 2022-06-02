using BonusService.Service.Model;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace BonusService.Service.DAL
{
    public class ReferralService
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<RebateService> _logger;
        private readonly WalletService walletService;
        private readonly WithdrawalLimitService withdrawalLimitService;
        public ReferralService(IConfiguration configuration, ILogger<RebateService> logger, WalletService walletService, WithdrawalLimitService withdrawalLimitService)
        {
            this.configuration = configuration;
            this.walletService = walletService;
            this.withdrawalLimitService = withdrawalLimitService;
            _logger = logger;
        }
        public IEnumerable<ReferralInfo> GetReferralSettlement()
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<ReferralInfo>("Service_ReferralSettlement_Get", commandType: CommandType.StoredProcedure);
        }

        public int PayoutReferral(IEnumerable<ReferralInfo> referralInfos, out decimal payAmount, out List<ReferralInfo> paidReferralList)
        {
            int result = 0;
            paidReferralList = new List<ReferralInfo>();
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            payAmount = 0m;
            long tc = DateTime.Now.Ticks;

            foreach (var rr in referralInfos)
            {
                try
                {
                    conn.Execute("Common_ReferralLog_Ins", new {
                        rr.CustId,
                        rr.CurrencyId,
                        rr.UserName,
                        rr.SiteId,
                        rr.AgentId, ReferralAmount = rr.BonusAmount, Memo = "", RelId = tc }, transaction: trans, commandType: CommandType.StoredProcedure);
                    payAmount += rr.BonusAmount;
                    paidReferralList.Add(rr);
                }
                catch (MySqlException mySqlEx)
                {
                    if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
                    {
                        continue;
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            if (payAmount > 0)
            {
                var agent = referralInfos.FirstOrDefault();
                result = walletService.SetBalance(agent.AgentId, agent.SiteId, agent.AgentName, payAmount, 13, $"Referral Bonus: {payAmount:#,##0.##}", "N/A", out _, out _, conn, trans);

                if (result > 0)
                {
                    conn.Execute("Common_ReferralMLog_Ins", new { CustId = agent.AgentId, agent.CurrencyId, UserName = agent.AgentName, agent.SiteId, ReferralAmount = payAmount, Memo = "", RelId = tc }, transaction: trans, commandType: CommandType.StoredProcedure);

                    var rollOver = 1 * payAmount;
                    if (rollOver > 0)
                        result = withdrawalLimitService.SetWithdrawalLimit(agent.AgentId, agent.AgentName, agent.SiteId, rollOver, $"Referral Bonus: {rollOver:#,##0.##}", "N/A", out _, conn, trans);

                    if (result > 0)
                    {
                        trans.Commit();
                    }
                }
            }

            return result;
        }
    }
}
