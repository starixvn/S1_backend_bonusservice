using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Data;
using BonusService.Service.Model;

namespace BonusService.Service.DAL
{
    public class BonusCashBackService
    {
        private IConfiguration configuration;
        private readonly WalletService walletService;

        public BonusCashBackService(IConfiguration configuration, WalletService walletService)
        {
            this.configuration = configuration;
            this.walletService = walletService;
        }

        public IEnumerable<BonusCashBack> GetBonusCashBackInfo()
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.Query<BonusCashBack>("Service_BonusCashBack_Get", commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<BonusCashBackGames> GetBonusCashBackGamesInfo(int CashbackId, int SiteId)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.Query<BonusCashBackGames>("Service_BonusCashBackGames_Get", new { CashbackId, SiteId }, commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<BonusCashBackMinMax> GetBonusCashBackMinMaxInfo(int CashbackId, int SiteId)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.Query<BonusCashBackMinMax>("Service_BonusCashBackMinMax_Get", new { CashbackId, SiteId }, commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<TpBetRecord> GetTpBetRecordInfo(long StartDate, long EndDate, int SiteId, int GameType)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.Query<TpBetRecord>("Service_BonusCashBackTpBetRecord_Get", new { StartDate, EndDate, SiteId, GameType }, commandType: CommandType.StoredProcedure);
            }
        }
        public int SetBonusCashBackLogoInfo(int GameType, string CustId, string UserName, int CurrencyId, int SiteId, string TpUserName, decimal StakeTotal, decimal WinlostAmountTotal)
        {


            return 0;

            //using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            //{
            //   return conn.Execute("Service_BonusCashBackLogoInfo_Ins", new { GameType, CustId, UserName, CurrencyId, SiteId, TpUserName, StakeTotal, WinlostAmountTotal }, commandType: CommandType.StoredProcedure);
            //}

        }

        public void InsBonusCashBack(int CustId, int SiteId, string UserName, decimal WinlostAmountTotal, int type)
        {
            int result = 0;
            byte is1stDepoist = 0;
            decimal afterAmount = 0;

            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                conn.Open();
                using var trans = conn.BeginTransaction();
                result = walletService.SetBalance(CustId, SiteId, UserName, WinlostAmountTotal, type,null,null, out afterAmount, out is1stDepoist, conn, trans);
                if (result > 0)
                {
                    trans.Commit();
                }
                else {
                    System.Console.WriteLine(12312312);
                }
            }
        }
    }
}
