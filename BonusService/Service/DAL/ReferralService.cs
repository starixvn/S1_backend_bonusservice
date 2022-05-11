using BonusService.Service.Model;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using BonusService.Service.DAL.Interface;
using BonusService.Service.Library;
using BonusService.Service.Model.enums;

namespace BonusService.Service.DAL
{
    public class ReferralService : IReferralService
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<RebateService> _logger;
        private readonly WalletService walletService;
        private readonly WithdrawalLimitService withdrawalLimitService;
        private readonly MySqlClient _reportDbClient;
        public ReferralService(IConfiguration configuration, ILogger<RebateService> logger, WalletService walletService, WithdrawalLimitService withdrawalLimitService, IEnumerable<MySqlClient> dbClients)
        {
            this.configuration = configuration;
            this.walletService = walletService;
            this.withdrawalLimitService = withdrawalLimitService;
            _reportDbClient = dbClients.FirstOrDefault(a => a.DataBase == DataBase.Report);
            _logger = logger;
        }
        public IEnumerable<ReferralInfo> GetReferralSettlement()
        {
            return _reportDbClient.Query<ReferralInfo>("Service_ReferralSettlement_Get");
        }

        public int GetRemainingCount(int agentId, int siteId, int currencyId)
        {
            var p = new DynamicParameters();
            p.Add("_AgentId", agentId);
            p.Add("_SiteId", siteId);
            p.Add("_CurrencyId", currencyId);
            return _reportDbClient.QueryFirst<int>("Service_ReferralRemainingCount_Get", p);
        }

        public int PayoutReferral(IEnumerable<ReferralInfo> referralInfos, out decimal payAmount, out List<ReferralInfo> paidReferralList)
        {
            int result = 0;
            paidReferralList = new List<ReferralInfo>();
            var agent = referralInfos.FirstOrDefault();
            payAmount = 0m;
            if (agent == null)
            {
                return result;
            }

            var remaining = GetRemainingCount(agent.AgentId, agent.SiteId, agent.CurrencyId);
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            var tc = DateTime.Now.Ticks;
            // 被推薦人領取獎勵
            foreach (var rr in referralInfos)
            {
                try
                {
                    if (remaining > 0)
                    {
                        result = walletService.SetBalance(rr.CustId, rr.SiteId, rr.UserName, rr.ReferralFreeCredit, (int)FinancialType.ReferFriendsRewards, $"Referral Free Credit: {rr.ReferralFreeCredit:#,##0.##}", "N/A", out _, out _, conn, trans);
                        result = withdrawalLimitService.SetWithdrawalLimit(rr.CustId, rr.UserName, rr.SiteId, rr.ReferralFreeCredit, $"Referral Free Credit: {rr.ReferralFreeCredit:#,##0.##}", "N/A", out _, conn, trans);
                        conn.Execute("Common_ReferralLog_Ins_V2",
                            new
                            {
                                CustId = rr.CustId, CurrencyId = rr.CurrencyId, UserName = rr.UserName,
                                SiteId = rr.SiteId, ReferralFreeCredit = rr.ReferralFreeCredit, 
                                AgentId = rr.AgentId, ReferralAmount = rr.BonusAmount, Memo = "",
                                RelId = tc, RewardStatus = 1
                            }, transaction: trans, commandType: CommandType.StoredProcedure);
                        payAmount += rr.BonusAmount;
                        paidReferralList.Add(rr);
                        remaining -= 1;
                    }
                    else
                    {
                        conn.Execute("Common_ReferralLog_Ins_V2",
                            new
                            {
                                CustId = rr.CustId, CurrencyId = rr.CurrencyId, UserName = rr.UserName,
                                SiteId = rr.SiteId, ReferralFreeCredit = rr.ReferralFreeCredit, AgentId = rr.AgentId,
                                ReferralAmount = rr.BonusAmount, Memo = "", RelId = tc, RewardStatus = 0
                            }, transaction: trans, commandType: CommandType.StoredProcedure);
                    }
                }
                catch (MySqlException mySqlEx)
                {
                    _logger.LogError(mySqlEx, $"{rr.CustId} payout referral free credit failed");
                    if (mySqlEx.Number == 1062) /*Duplicate for key 'PRIMARY'*/
                    {
                        continue;
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // 推薦人領取獎勵
            if (payAmount > 0)
            {
                
                result = walletService.SetBalance(agent.AgentId, agent.SiteId, agent.AgentName, payAmount, (int)FinancialType.ReferFriendsRewards, $"Referral Bonus: {payAmount:#,##0.##}", "N/A", out _, out _, conn, trans);

                if (result > 0)
                {
                    conn.Execute("Common_ReferralMLog_Ins", new { CustId = agent.AgentId, CurrencyId = agent.CurrencyId, UserName = agent.AgentName, SiteId = agent.SiteId, ReferralAmount = payAmount, Memo = "", RelId = tc }, transaction: trans, commandType: CommandType.StoredProcedure);

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
