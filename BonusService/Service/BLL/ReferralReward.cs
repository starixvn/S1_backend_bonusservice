using BonusService.Service.DAL.Interface;
using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using MySql.Data.MySqlClient;

namespace BonusService.Service.BLL
{
    public class ReferralReward
    {
        private readonly IReferralService _referralService;
        private readonly IWalletAdapter _walletAdapter;
        private readonly IWithdrawalLimitAdapter _withdrawalLimitAdapter;
        private readonly MySqlClient _mainDbClient;
        private readonly ILogger<ReferralReward> _logger;

        public ReferralReward(IReferralService referralService, IWalletAdapter walletAdapter, IWithdrawalLimitAdapter withdrawalLimitAdapter, IEnumerable<MySqlClient> dbClients, ILogger<ReferralReward> logger)
        {
            _referralService = referralService;
            _walletAdapter = walletAdapter;
            _withdrawalLimitAdapter = withdrawalLimitAdapter;
            _mainDbClient = dbClients.FirstOrDefault(a => a.DataBase == DataBase.Main);
            _logger = logger;
        }
        
        public IEnumerable<ReferralInfo> GetReferralSettlement()
        {
            return _referralService.GetReferralSettlement();
        }
        
        public int Payout(IEnumerable<ReferralInfo> referralInfos, out decimal payAmount, out List<ReferralInfo> paidReferralList)
        {
            var result = 0;
            paidReferralList = new List<ReferralInfo>();
            var agent = referralInfos.FirstOrDefault();
            payAmount = 0m;
            if (agent == null)
            {
                return result;
            }

            var remaining = _referralService.GetRemainingCount(agent.AgentId, agent.SiteId, agent.CurrencyId);
            using var conn = _mainDbClient.GetMySqlConnection();
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
                        result = _walletAdapter.SetBalance(rr.CustId, rr.SiteId, rr.UserName, rr.ReferralFreeCredit, (int)FinancialType.ReferFriendsRewards, $"Referral Free Credit: {rr.ReferralFreeCredit:#,##0.##}", "N/A", out _, out _, trans);
                        if (result > 0)
                        {
                            _mainDbClient.Execute("Common_ReferralLog_Ins_V2",trans,
                                new
                                {
                                    CustId = rr.CustId, CurrencyId = rr.CurrencyId, UserName = rr.UserName,
                                    SiteId = rr.SiteId, ReferralFreeCredit = rr.ReferralFreeCredit, AgentId = rr.AgentId,
                                    ReferralAmount = rr.BonusAmount, Memo = "", RelId = tc, RewardStatus = 1
                                });
                            
                            result = _withdrawalLimitAdapter.SetWithdrawalLimit(rr.CustId, rr.UserName, rr.SiteId, rr.ReferralFreeCredit, $"Referral Free Credit: {rr.ReferralFreeCredit:#,##0.##}", "N/A",(int) WithdrawalLimitLogCreateFromEnum.ReferralReward, "System" , out _, trans);
                            payAmount += rr.BonusAmount;
                            paidReferralList.Add(rr);
                            remaining -= 1;
                        }
                    }
                    else
                    {
                        result = _mainDbClient.Execute("Common_ReferralLog_Ins_V2",trans,
                            new
                            {
                                CustId = rr.CustId, CurrencyId = rr.CurrencyId, UserName = rr.UserName,
                                SiteId = rr.SiteId, ReferralFreeCredit = 0m, AgentId = rr.AgentId,
                                ReferralAmount = 0m, Memo = "", RelId = tc, RewardStatus = 0
                            });
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
                
                result = _walletAdapter.SetBalance(agent.AgentId, agent.SiteId, agent.AgentName, payAmount, (int)FinancialType.ReferFriendsRewards, $"Referral Bonus: {payAmount:#,##0.##}", "N/A", out _, out _, trans);

                if (result > 0)
                {
                    _mainDbClient.Execute("Common_ReferralMLog_Ins", trans ,new { CustId = agent.AgentId, CurrencyId = agent.CurrencyId, UserName = agent.AgentName, SiteId = agent.SiteId, ReferralAmount = payAmount, Memo = "", RelId = tc });

                    var rollOver = 1 * payAmount;
                    if (rollOver > 0)
                        result = _withdrawalLimitAdapter.SetWithdrawalLimit(agent.AgentId, agent.AgentName, agent.SiteId, rollOver, $"Referral Bonus: {rollOver:#,##0.##}", "N/A", (int) WithdrawalLimitLogCreateFromEnum.ReferralReward, "System" ,out _, trans);
                }
            }
            
            if (result > 0)
            {
                trans.Commit();
            }

            conn.Close();
            conn.Dispose();
            return result;
        }
    }
}