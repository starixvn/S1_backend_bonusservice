using BonusService.Service.Library;
using BonusService.Service.Model;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace BonusService.Service.DAL
{
    public class VipRebateService
    {
        private readonly IConfiguration configuration;
        private readonly WalletService walletService;
        private readonly WithdrawalLimitService withdrawalLimitService;
        private readonly Common common;
        public VipRebateService(IConfiguration configuration, WalletService walletService, Common common, WithdrawalLimitService withdrawalLimitService)
        {
            this.configuration = configuration;
            this.walletService = walletService;
            this.withdrawalLimitService = withdrawalLimitService;
            this.common = common;
        }
        public IEnumerable<VipRebateTpBetRecord> GetVipRebateTpBetRecord(int siteId, int custId, int is14)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<VipRebateTpBetRecord>("Service_VipRebateTpBetRecord_Get", new { SiteId = siteId, CustId = custId, Is14 = is14 }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<VipRebateInfo> GetVipRebateCustList(int siteId, int pageSize, int pageNumber)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<VipRebateInfo>("Service_VipRebateCustList_Get", new { SiteId = siteId, PageSize = pageSize, PageNumber = pageNumber }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<RebateTask> GetVipRebateTask()
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<RebateTask>("Service_VipRebateTask_Get", commandType: CommandType.StoredProcedure);
        }
        public int PayoutVipRebate(int siteId, int custId, string userName, VipRebateInfo rebateInfo, IEnumerable<VipRebateTpBetRecord> rebateTpBetRecords, out decimal payAmount, out Dictionary<int, decimal> rebateLogList, out Dictionary<int, decimal> rebateCLogList, IEnumerable<RebatePayoutLimit> rebatePayoutLimits)
        {
            int result = 0;
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            payAmount = 0m;
            int promotionType = 0;
            rebateLogList = new Dictionary<int, decimal>();
            rebateCLogList = new Dictionary<int, decimal>();

            long tc = DateTime.Now.Ticks;

            foreach (var rr in rebateTpBetRecords)
            {
                var sum = rr.ValidBet;
                if (sum != 0)
                {
                    var rebateRecord = GetRebateRecord(custId, int.Parse(rr.SettledDate.ToString("yyyyMMdd")), rr.GameType, 6, conn, trans);
                    if (rebateRecord == null)
                    {
                        promotionType = 6;
                        SetRebateRecord(custId, int.Parse(rr.SettledDate.ToString("yyyyMMdd")), rr.GameType, sum, promotionType, tc, conn, trans);
                        var amount = sum;
                        amount = common.Floor(amount, 2);

                        if (rebateLogList.ContainsKey(rr.GameType))
                        {
                            rebateLogList[rr.GameType] += amount;
                        }
                        else
                        {
                            rebateLogList.Add(rr.GameType, amount);
                        }

                        if (rebateCLogList.ContainsKey(rr.CategoryId))
                        {
                            rebateCLogList[rr.CategoryId] += amount;
                        }
                        else
                        {
                            rebateCLogList.Add(rr.CategoryId, amount);
                        }
                    }
                }
            }

            foreach (var rebateKey in rebateCLogList.Keys)
            {
                var rebatePayoutLimit = rebatePayoutLimits.Where(x => x.CurrencyId == rebateInfo.CurrencyId && x.SiteId == siteId && x.CategoryId == rebateKey).FirstOrDefault();
                if (rebatePayoutLimit == null)
                {
                    if (rebateCLogList[rebateKey] > rebatePayoutLimit.MinPayout)
                    {
                        if (rebatePayoutLimit.MaxPayout != 0 && rebatePayoutLimit.MaxPayout < rebateCLogList[rebateKey])
                        {
                            rebateCLogList[rebateKey] = rebatePayoutLimit.MaxPayout;
                        }
                    }
                    else
                    {
                        rebateCLogList[rebateKey] = 0;
                    }
                }

                payAmount += rebateCLogList[rebateKey];
            }

            if (payAmount > 0)
            {
                result = walletService.SetBalance(custId, siteId, userName, payAmount, 17 - promotionType, $"{(promotionType == 6 ? "Rebate" : "Cashback")}: {payAmount:#,##0.##}", "", out _, out _, conn, trans);

                if (result > 0)
                {
                    conn.Execute("Common_RebateMLog_Ins", new { CustId = custId, CurrencyId = rebateInfo.CurrencyId, RebateType = promotionType - 5, UserName = userName, SiteId = siteId, RedeemAmount = payAmount, Memo = "", BonusCode = (string)null, RelId = tc }, transaction: trans, commandType: CommandType.StoredProcedure);
                    int rebateSetting1_RollOver = 0;
                    var rollOver = rebateSetting1_RollOver * payAmount;
                    if (rollOver > 0)
                        result = withdrawalLimitService.SetWithdrawalLimit(custId, userName, siteId, rollOver, $"{(promotionType == 6 ? "Rebate" : "Cashback")}: {rollOver:#,##0.##}", "", out _, conn, trans);

                    if (result > 0)
                    {
                        foreach (var rebateLog in rebateLogList)
                        {
                            conn.Execute("Common_RebateLog_Ins", new { CustId = custId, CurrencyId = rebateInfo.CurrencyId, GameType = rebateLog.Key, RebateType = promotionType - 5, UserName = userName, SiteId = siteId, RedeemAmount = rebateLog.Value, Memo = "", BonusCode = (string)null, RelId = tc }, transaction: trans, commandType: CommandType.StoredProcedure);
                        }

                        foreach (var rebateLog in rebateCLogList)
                        {
                            conn.Execute("Common_RebateCLog_Ins", new { CustId = custId, CurrencyId = rebateInfo.CurrencyId, CategoryId = rebateLog.Key, RebateType = promotionType - 5, UserName = userName, SiteId = siteId, RedeemAmount = rebateLog.Value, Memo = "", RelId = tc }, transaction: trans, commandType: CommandType.StoredProcedure);
                        }

                        trans.Commit();
                    }
                }
            }

            return result;
        }
        public RebateRecord GetRebateRecord(int CustId, int SettledDate, int GameType, int PromotionType, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.Query<RebateRecord>("Service_RebateRecord_Get", new { CustId, SettledDate, GameType, BonusCode = (string)null, PromotionType }, transaction: trans, commandType: CommandType.StoredProcedure).FirstOrDefault();
        }
        public int SetRebateRecord(int CustId, int SettledDate, int GameType, decimal Sum, int PromotionType, long RelId, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.Execute("Service_RebateRecord_Set", new { CustId, SettledDate, GameType, BonusCode = (string)null, Sum, PromotionType, RelId }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<RebatePayoutLimit> GetRebatePayoutLimit()
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<RebatePayoutLimit>("Service_RebatePayoutLimit_Get", commandType: CommandType.StoredProcedure);
        }
        public int SetVipRebateTaskDone(int siteId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_VipRebateTaskDone_Set", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
    }
}
