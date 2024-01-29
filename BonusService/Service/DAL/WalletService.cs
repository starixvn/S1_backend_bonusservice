using System.Data;

using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;

using Dapper;

using MySql.Data.MySqlClient;

using Newtonsoft.Json;

namespace BonusService.Service.DAL
{
    public class WalletService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly ILogger<WalletService> _logger;

        public WalletService(IServiceProvider serviceProvider)
        {
            _mainDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _logger = serviceProvider.GetRequiredService<ILogger<WalletService>>();
        }

        public int SetBalance(int custId, int siteId, string userName, decimal amount, int type, string memo,
            string refTransId, out decimal afterAmount, out byte is1StDeposit, MySqlTransaction trans)
        {
            try
            {
                var p = new DynamicParameters(new
                {
                    CustId = custId,
                    SiteId = siteId,
                    UserName = userName,
                    Amount = amount,
                    Type = type,
                    Memo = memo,
                    RefTransId = refTransId
                });
                p.Add("Is1stDepoist", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                p.Add("AfterAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                _logger.LogInformation($"SetBalance Input {JsonConvert.SerializeObject(p)}");
                var result = _mainDbClient.Execute("Wallet_Set_V2", trans, p);
                is1StDeposit = p.Get<byte?>("Is1stDepoist") ?? 0;
                afterAmount = p.Get<decimal?>("AfterAmount") ?? 0;
                _logger.LogInformation($"SetBalance Result AfterAmount:{afterAmount}, Result : {result} , Memo : {memo}");
                return afterAmount >= 0 ? 1 : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"SetBalance Error : {ex.Message}");
                throw;
            }
        }


        public decimal GetBalance(int custId)
        {
            return _reportDbClient.ExecuteScalar<decimal>("Balance_Get", new { CustId = custId });
        }
        
        public IEnumerable<BonusFulfill> GetBonusFulfill()
        {            
            return _reportDbClient.Query<BonusFulfill>("Service_BonusFulfill_Get_V2");
        }

        public int BonusSettle(BonusFulfill bonusFulfill)
        {
            try
            {
                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();
     
                var p = new DynamicParameters(bonusFulfill);
                p.Add("Bonus", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                p.Add("WalletType", dbType: DbType.Int32, direction: ParameterDirection.Output);
                var result = _mainDbClient.Execute("Service_BonusSettle_Set_V4", trans,p);
                        
                if (result > 0) 
                {
                    var bonus = p.Get<decimal>("Bonus");
                    var walletType = p.Get<int>("WalletType");

                    // 單一錢包的bonus有餘額進行Fulfill轉回Bonus餘額回主錢包
                    if (walletType == 1 && bonus > 0)
                    {                        
                        result = SetBalance(
                            bonusFulfill.CustId, 
                            bonusFulfill.SiteId,
                            bonusFulfill.UserName, 
                            bonus, 
                            (int)FinancialType.Bonusreward,
                            $"Bonus Transfer to Cash: {Math.Abs(bonus):#,##0.##}",
                            bonusFulfill.BonusId.ToString(),
                            out _, 
                            out _, trans);
                    }

                    if (result > 0)
                    {
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                    }
                }
                return result;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw;
            }
        }
    }

}
