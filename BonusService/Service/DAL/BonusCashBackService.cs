using BonusService.Service.Library;
using BonusService.Service.Model;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace BonusService.Service.DAL
{
    public class BonusCashBackService
    {
        private IConfiguration configuration;
        private readonly WalletService walletService;
        private readonly Common common;

        public BonusCashBackService(IConfiguration configuration, WalletService walletService,Common common)
        {
            this.configuration = configuration;
            this.walletService = walletService;
            this.common = common;
        }

        public IEnumerable<BonusCashBack> GetBonusCashBackInfo()
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString_Report")))
            {
                return conn.Query<BonusCashBack>("Service_BonusCashBack_Get", commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<BonusCashBackGames> GetBonusCashBackGamesInfo(int CashbackId, int SiteId)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString_Report")))
            {
                return conn.Query<BonusCashBackGames>("Service_BonusCashBackGames_Get", new { CashbackId, SiteId }, commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<BonusCashBackMinMax> GetBonusCashBackMinMaxInfo(int CashbackId, int SiteId)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString_Report")))
            {
                return conn.Query<BonusCashBackMinMax>("Service_BonusCashBackMinMax_Get", new { CashbackId, SiteId }, commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<TpBetRecord> GetTpBetRecordInfo(long StartDate, long EndDate, int SiteId, int GameType)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString_Report")))
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
        public int InsBonusCashBack(BonusCashBackInfoList bonusCashBackInfo)
        {
            int result = 0;
            byte is1stDepoist = 0;
            decimal afterAmount = 0;

            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                conn.Open();
                using var trans = conn.BeginTransaction();
                result = walletService.SetBalance(bonusCashBackInfo.CustId, bonusCashBackInfo.SiteId, bonusCashBackInfo.UserName, bonusCashBackInfo.WinlostAmountCountCalculationTotal, 7, null, null, out afterAmount, out is1stDepoist, conn, trans);
                if (result > 0)
                {
                    string BonusInfo = null;
                    string Memo = null;
                    string Prefix = $"B{common.GetEnv()}";
                    int ExpiryDays = 3;

                    //Console.WriteLine(
                    //    $"{BonusCashBackInfoList.CustId}  " +
                    //    $"{BonusCashBackInfoList.UserName}  " +
                    //    $"{BonusCashBackInfoList.CurrencyId}  " +
                    //    $"{BonusCashBackInfoList.SiteId}  " +
                    //    $"{BonusInfo}  " +
                    //    $"{afterAmount}  " +
                    //    $"{BonusCashBackInfoList.WinlostAmountCountCalculationTotal}  " +
                    //    $"{ExpiryDays}  " +
                    //    $"{Memo}  " +
                    //    $"{Prefix} "
                    //);

                    var p = new DynamicParameters(new { PromotionType = 4, BonusCode = "CashBack", bonusCashBackInfo.CustId, bonusCashBackInfo.UserName, bonusCashBackInfo.CurrencyId, bonusCashBackInfo.SiteId, BonusInfo, LockedAmount = afterAmount, BonusAmount = bonusCashBackInfo.WinlostAmountCountCalculationTotal, ExpiryDays, Memo, Prefix });
                    p.Add("BonusId", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    result = conn.Execute("Service_BonusInfoCashBack_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
                    if (result > 0)
                    {
                        var BonusId = p.Get<int>("BonusId");
                        foreach (var BonusCashBackInfoListdetail in bonusCashBackInfo.BonusCashBackInfoListDetails)
                        {

                            //Console.WriteLine(
                            //    $"{BonusId}  " +
                            //    $"{BonusCashBackInfoList.CustId}  " +
                            //    $"{BonusCashBackInfoListdetail.GameType}  " +
                            //    $"{BonusCashBackInfoList.UserName}  " +
                            //    $"{BonusCashBackInfoList.CurrencyId}  " +
                            //    $"{BonusCashBackInfoList.SiteId}  " +
                            //    $"{BonusCashBackInfoListdetail.StakeCount}  " +
                            //    $"{BonusCashBackInfoListdetail.WinlostAmountCount}  " +
                            //    $"{BonusCashBackInfoListdetail.WinlostAmountCountCalculation}  " +
                            //    $"{BonusCashBackInfoListdetail.Percentage}  "

                            //);
                            conn.Execute("Service_BonusCashBackInfo_Ins", new { BonusInfoId = BonusId, bonusCashBackInfo.CustId, GameId = BonusCashBackInfoListdetail.GameType, bonusCashBackInfo.UserName, bonusCashBackInfo.CurrencyId, bonusCashBackInfo.SiteId, BonusCashBackInfoListdetail.StakeCount, BonusCashBackInfoListdetail.WinlostAmountCount, BonusCashBackInfoListdetail.WinlostAmountCountCalculation, BonusCashBackInfoListdetail.Percentage }, transaction: trans, commandType: CommandType.StoredProcedure);
                        }

                        trans.Commit();
                    }
                    //Console.WriteLine(
                    //    $"{BonusCashBackInfoList.CustId}  " +
                    //    $"{BonusCashBackInfoList.SiteId}  " +
                    //    $"{BonusCashBackInfoList.UserName}  " +
                    //    $"{BonusCashBackInfoList.WinlostAmountCountCalculationTotal}  " +
                    //    $"{afterAmount}  " +
                    //    $"{is1stDepoist} "
                    //);
                }
            }


            return result;
        }
    }
}
