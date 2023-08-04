using BonusService.Service.DAL;
using BonusService.Service.DAL.Interface;
using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace BonusService.Service.BLL
{
    public class ReferralReward
    {
        private readonly IReferralService _referralService;
        private readonly IWalletAdapter _walletAdapter;
        private readonly IWithdrawalLimitAdapter _withdrawalLimitAdapter;
        private readonly MySqlClient _mainDbClient;
        private readonly ILogger<ReferralReward> _logger;

        public ReferralReward(IReferralService referralService,
            IWalletAdapter walletAdapter,
            IWithdrawalLimitAdapter withdrawalLimitAdapter,
            IEnumerable<MySqlClient> dbClients,

            ILogger<ReferralReward> logger)
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
                            _mainDbClient.Execute("Common_ReferralLog_Ins_V3", trans,
                                new
                                {
                                    CustId = rr.CustId,
                                    CurrencyId = rr.CurrencyId,
                                    Type = ReferralTypeEnum.Referral,
                                    UserName = rr.UserName,
                                    SiteId = rr.SiteId,
                                    ReferralFreeCredit = rr.ReferralFreeCredit,
                                    AgentId = rr.AgentId,
                                    ReferralAmount = rr.BonusAmount,
                                    CalculateSummary = 0m,
                                    RewardPercentage = 0m,
                                    RefList = JsonConvert.SerializeObject(""),
                                    SettledDate = 0,
                                    Memo = "",
                                    RelId = tc,
                                    RewardStatus = ReferralStatusEnum.AllowReward
                                });

                            result = _withdrawalLimitAdapter.SetWithdrawalLimit(rr.CustId, rr.UserName, rr.SiteId, rr.ReferralFreeCredit, $"Referral Free Credit: {rr.ReferralFreeCredit:#,##0.##}", "N/A", (int)WithdrawalLimitLogCreateFromEnum.ReferralReward, "System", out _, trans);
                            payAmount += rr.BonusAmount;
                            paidReferralList.Add(rr);
                            remaining -= 1;
                        }
                    }
                    else
                    {
                        result = _mainDbClient.Execute("Common_ReferralLog_Ins_V3", trans,
                            new
                            {
                                CustId = rr.CustId,
                                CurrencyId = rr.CurrencyId,
                                Type = ReferralTypeEnum.Referral,
                                UserName = rr.UserName,
                                SiteId = rr.SiteId,
                                ReferralFreeCredit = 0m,
                                AgentId = rr.AgentId,
                                ReferralAmount = 0m,
                                CalculateSummary = 0m,
                                RewardPercentage = 0m,
                                RefList = JsonConvert.SerializeObject(""),
                                SettledDate = 0,
                                Memo = "",
                                RelId = tc,
                                RewardStatus = ReferralStatusEnum.NotAllowReward
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
                    _mainDbClient.Execute("Common_ReferralMLog_Ins_V2", trans,
                        new
                        {
                            CustId = agent.AgentId,
                            CurrencyId = agent.CurrencyId,
                            Type = ReferralTypeEnum.Referral,
                            UserName = agent.AgentName,
                            SiteId = agent.SiteId,
                            ReferralAmount = payAmount,
                            CalculateAmount = 0,
                            MinPayout = 0m,
                            MaxPayout = 0m,
                            Memo = "",
                            RelId = tc
                        });

                    var rollOver = 1 * payAmount;
                    if (rollOver > 0)
                        result = _withdrawalLimitAdapter.SetWithdrawalLimit(agent.AgentId, agent.AgentName, agent.SiteId, rollOver, $"Referral Bonus: {rollOver:#,##0.##}", "N/A", (int)WithdrawalLimitLogCreateFromEnum.ReferralReward, "System", out _, trans);
                }
            }

            if (result > 0)
            {
                _mainDbClient.CommitAndDisposeConnection(conn, trans);
            }
            else
            {
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            }

            return result;
        }

        public int DepositReferralPayout(int siteId, int currencyId, DepositReferralReferrerCaculateResult caculateResult)
        {
            var result = 0;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var tc = DateTime.Now.Ticks;
            // 被推薦人領取獎勵：DepositReferral的機制，被推薦人沒有領取任何獎勵，但是需要log排查計算歷程
            foreach (var referredInfo in caculateResult.ReferredDetail)
            {
                try
                {
                    result = _mainDbClient.Execute("Common_ReferralLog_Ins_V3", trans,
                        new
                        {
                            CustId = referredInfo.Referred_CustId,
                            CurrencyId = currencyId,
                            Type = ReferralTypeEnum.DepositReferral,
                            UserName = referredInfo.Referred_UserName,
                            SiteId = siteId,
                            ReferralFreeCredit = 0m,
                            AgentId = caculateResult.Referrer_CustId,
                            ReferralAmount = referredInfo.CaculateDepositReferral,
                            CalculateSummary = referredInfo.TotalDeposit,
                            RewardPercentage = referredInfo.DepositRewardPercentage,
                            RefList = JsonConvert.SerializeObject(referredInfo.DepositInfo.Select(x => x.Key)),
                            caculateResult.SettledDate,
                            Memo = "DepositReferral",
                            RelId = tc,
                            RewardStatus = 0
                        });
                }
                catch (MySqlException mySqlEx)
                {
                    _logger.LogError(mySqlEx, $"{referredInfo.Referred_CustId} payout referral free credit failed");
                    if (mySqlEx.Number == 1062) /*Duplicate for key 'PRIMARY'*/
                        continue;
                    else
                        throw;
                }
            }

            // 推薦人領取獎勵

            if (result > 0 && caculateResult.ReferredDetail.Any())
            {
                result = _mainDbClient.Execute("Common_ReferralMLog_Ins_V2", trans,
                    new
                    {
                        CustId = caculateResult.Referrer_CustId,
                        CurrencyId = currencyId,
                        Type = ReferralTypeEnum.DepositReferral,
                        UserName = caculateResult.Referrer_UserName,
                        SiteId = siteId,
                        ReferralAmount = caculateResult.RewardDepositReferral,
                        CalculateAmount = caculateResult.CaculateDepositReferral,
                        caculateResult.MinPayout,
                        caculateResult.MaxPayout,
                        Memo = "DepositReferral",
                        RelId = tc
                    });

                if (result > 0 && caculateResult.RewardDepositReferral > 0)
                {
                    result = _walletAdapter.SetBalance(caculateResult.Referrer_CustId,
                        siteId,
                        caculateResult.Referrer_UserName,
                        caculateResult.RewardDepositReferral,
                        (int)FinancialType.ReferFriendsRewards,
                        $"Referral Bonus: {caculateResult.RewardDepositReferral:#,##0.##}", "N/A", out _, out _, trans);

                    if (result > 0)
                    {
                        var rollOver = 1 * caculateResult.RewardDepositReferral;
                        if (rollOver > 0)
                            result = _withdrawalLimitAdapter.SetWithdrawalLimit(
                                caculateResult.Referrer_CustId,
                                caculateResult.Referrer_UserName,
                                siteId,
                                rollOver,
                                $"Referral Bonus: {rollOver:#,##0.##}",
                                "N/A",
                                (int)WithdrawalLimitLogCreateFromEnum.ReferralReward,
                                "System",
                                out _, trans);
                    }
                }
            }

            if (result > 0)
                _mainDbClient.CommitAndDisposeConnection(conn, trans);
            else
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);

            return result;
        }
    }
}