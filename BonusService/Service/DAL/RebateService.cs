using BonusService.Service.Library;
using BonusService.Service.Model;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Data;

namespace BonusService.Service.DAL
{
    public class RebateService
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<RebateWorker> _logger;
        private readonly WalletService walletService;
        private readonly WithdrawalLimitService withdrawalLimitService;
        private readonly Common common;

        public RebateService(IConfiguration configuration, WalletService walletService, Common common, ILogger<RebateWorker> logger, WithdrawalLimitService withdrawalLimitService)
        {
            this.configuration = configuration;
            this.walletService = walletService;
            this.withdrawalLimitService = withdrawalLimitService;
            this.common = common;
            _logger = logger;
        }

        public IEnumerable<RebateTask> GetRebateTask()
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<RebateTask>("Service_RebateTask_Get", commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<RebateInfo> GetRebateInfo(string bonusCode, int siteId, int pageSize, int pageNumber)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<RebateInfo>("Service_RebateInfo_Get", new { BonusCode = bonusCode, SiteId = siteId, PageSize = pageSize, PageNumber = pageNumber }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<RebateTpBetRecord> GetRebateTpBetRecord(string bonusCode, int siteId, int custId, int currencyId, int is14)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<RebateTpBetRecord>("Service_RebateTpBetRecord_Get", new { BonusCode = bonusCode, SiteId = siteId, CustId = custId, Is14 = is14 }, commandType: CommandType.StoredProcedure);
        }
        public int PayoutRebate(int siteId, int custId, string userName, decimal payAmount, string bonusCode)
        {
            int result = 0;
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            result = walletService.SetBalance(custId, siteId, userName, payAmount, 8, $"{payAmount:#,##0.##}", bonusCode, out _, out _, conn, trans);
            if (result > 0)
            {
                result = withdrawalLimitService.SetWithdrawalLimit(custId, userName, siteId, payAmount, $"{payAmount:#,##0.##}", bonusCode, out _, conn, trans);
                if (result > 0)
                {
                    trans.Commit();
                }
            }

            return result;
        }

        //                    result = walletService.SetBalance(bonusCashBackInfo.CustId, bonusCashBackInfo.SiteId, bonusCashBackInfo.UserName, WinlostAmountCountCalculationTotal, 7, memo, null, out afterAmount, out is1stDepoist, conn, trans);
        public IEnumerable<BonusCashBack> GetBonusCashBackInfo()
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport")))
            {
                return conn.Query<BonusCashBack>("Service_BonusCashBack_Get", commandType: CommandType.StoredProcedure);
            }
        }
        public IEnumerable<BonusCashBackGames> GetBonusCashBackGamesInfo(int CashbackId, int SiteId)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport")))
            {
                return conn.Query<BonusCashBackGames>("Service_BonusCashBackGames_Get", new { CashbackId, SiteId }, commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<BonusCashBackMinMax> GetBonusCashBackMinMaxInfo(int CashbackId, int SiteId)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport")))
            {
                return conn.Query<BonusCashBackMinMax>("Service_BonusCashBackMinMax_Get", new { CashbackId, SiteId }, commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<TpBetRecord> GetTpBetRecordInfo(long StartDate, long EndDate, int SiteId, int GameType)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport")))
            {
                return conn.Query<TpBetRecord>("Service_BonusCashBackTpBetRecord_Get", new { StartDate, EndDate, SiteId, GameType }, commandType: CommandType.StoredProcedure);
            }
        }
        public void SetBonusCashBackLogoInfo(int BonusInfoId, int GameId, int CustId, string UserName, int CurrencyId, int SiteId, decimal StakeCount, decimal WinlostAmountCount, decimal Percentage)
        {

            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                conn.Execute("Service_BonusCashBackLogoInfo_Ins", new { BonusInfoId, CustId, GameId, UserName, CurrencyId, SiteId, StakeCount, WinlostAmountCount, Percentage }, commandType: CommandType.StoredProcedure);
            }
        }
        public void SetBonusInfoCashBack(int PromotionType, string BonusCode, int CustId, string UserName, int CurrencyId, int SiteId, decimal LockedAmount, decimal BonusAmount, decimal ExpiryDays)
        {

            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                string BonusInfo = "";
                string Memo = "";
                string Prefix = $"B{common.GetEnv()}";

                var p = new DynamicParameters(new { PromotionType, BonusCode, CustId, UserName, CurrencyId, SiteId, BonusInfo, LockedAmount, BonusAmount, ExpiryDays, Memo, Prefix });
                p.Add("BonusId", dbType: DbType.String, direction: ParameterDirection.Output);

                conn.Execute("Service_BonusInfoCashBack_Ins", p, commandType: CommandType.StoredProcedure);
            }

        }

        public int GetBonusCashBack(BonusCashBackInfoList bonusCashBackInfo, out decimal WinlostAmount, MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new { bonusCashBackInfo.CustId, bonusCashBackInfo.CalculationDay, bonusCashBackInfo.WinlostAmountCountCalculationTotal });
            p.Add("WinlostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            int result = conn.Execute("Service_BonusInfoCashBack_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
            WinlostAmount = p.Get<decimal>("WinlostAmount");
            return result;
        }

        public int InsBonusCashBack(BonusCashBackInfoList bonusCashBackInfo)
        {
            int result = 0;
            byte is1stDepoist = 0;
            decimal afterAmount = 0;

            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                conn.Open();
                using var trans = conn.BeginTransaction();
                string memo = null;
                decimal WinlostAmountCountCalculationTotal = 0;

                try
                {
                    result = GetBonusCashBack(bonusCashBackInfo, out WinlostAmountCountCalculationTotal, conn, trans);
                }
                catch (MySqlException mySqlEx)
                {
                    if (mySqlEx.Number == 3572)
                    {
                        return 1;
                    }
                    else
                    {
                        throw mySqlEx;
                    }
                }



                if (WinlostAmountCountCalculationTotal != bonusCashBackInfo.WinlostAmountCountCalculationTotal && result > 0)
                {
                    _logger.LogInformation(
                   $" WinlostAmountCountCalculationTotal:{WinlostAmountCountCalculationTotal}" +
                   $" bonusCashBackInfo WinlostAmountCountCalculationTotal:{bonusCashBackInfo.WinlostAmountCountCalculationTotal}" +
                   $" result:{result}"
               );
                    if (WinlostAmountCountCalculationTotal > 0)
                    {
                        memo = "ReCalculationTotal";
                    }
                    WinlostAmountCountCalculationTotal = bonusCashBackInfo.WinlostAmountCountCalculationTotal - WinlostAmountCountCalculationTotal;
                    decimal WithdrawBonusCashBack = WinlostAmountCountCalculationTotal * bonusCashBackInfo.WithdrawCondition;

                    result = walletService.SetBalance(bonusCashBackInfo.CustId, bonusCashBackInfo.SiteId, bonusCashBackInfo.UserName, WinlostAmountCountCalculationTotal, 7, memo, null, out afterAmount, out is1stDepoist, conn, trans);
                    _logger.LogInformation(result.ToString());

                    if (result > 0)
                    {
                        string BonusInfo = null;
                        string Memo = null;
                        string Prefix = $"B{common.GetEnv()}";
                        int ExpiryDays = 3;

                        var p = new DynamicParameters(new { PromotionType = 4, BonusCode = "CashBack", bonusCashBackInfo.CustId, bonusCashBackInfo.UserName, bonusCashBackInfo.CurrencyId, bonusCashBackInfo.SiteId, BonusInfo, LockedAmount = afterAmount, BonusAmount = bonusCashBackInfo.WinlostAmountCountCalculationTotal, ExpiryDays, Memo, bonusCashBackInfo.CalculationDay, Prefix });
                        p.Add("BonusId", dbType: DbType.Int32, direction: ParameterDirection.Output);
                        p.Add("TransId", dbType: DbType.String, direction: ParameterDirection.Output);
                        result = conn.Execute("Service_BonusInfoCashBack_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                        if (result > 0)
                        {
                            var TransId = p.Get<string>("TransId");
                            decimal withdrawalLimit = 0;
                            result = withdrawalLimitService.SetWithdrawalLimit(bonusCashBackInfo.CustId, bonusCashBackInfo.UserName, bonusCashBackInfo.SiteId, WithdrawBonusCashBack, $"BonusCashBack: {WithdrawBonusCashBack}", TransId, out withdrawalLimit, conn, trans);
                        }
                        if (result > 0)
                        {
                            var BonusId = p.Get<int>("BonusId");
                            foreach (var BonusCashBackInfoListdetail in bonusCashBackInfo.BonusCashBackInfoListDetails)
                            {
                                conn.Execute("Service_BonusCashBackInfo_Ins", new { BonusInfoId = BonusId, bonusCashBackInfo.CustId, GameId = BonusCashBackInfoListdetail.GameType, bonusCashBackInfo.UserName, bonusCashBackInfo.CurrencyId, bonusCashBackInfo.SiteId, BonusCashBackInfoListdetail.StakeCount, BonusCashBackInfoListdetail.WinlostAmountCount, BonusCashBackInfoListdetail.WinlostAmountCountCalculation, BonusCashBackInfoListdetail.Percentage }, transaction: trans, commandType: CommandType.StoredProcedure);
                            }
                            trans.Commit();
                        }
                    }
                }
            }

            return result;
        }
    }
}
