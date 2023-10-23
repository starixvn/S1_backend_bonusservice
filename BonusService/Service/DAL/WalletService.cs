using Dapper;
using MySql.Data.MySqlClient;
using System.Data;
using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using Newtonsoft.Json;

namespace BonusService.Service.DAL
{
    public class WalletService
    {
        private const int typePosition = 10000;
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
            _mainDbClient.Execute("Wallet_Set_V2", trans, p);
            is1StDeposit = p.Get<byte?>("Is1stDepoist") ?? 0;
            afterAmount = p.Get<decimal?>("AfterAmount") ?? 0;
            return afterAmount >= 0 ? 1 : 0;
        }

         public int SetBalance(int custId, int siteId, string userName, decimal Amount, int tpId, TransType transType, string Memo, string RefTransId, out decimal afterAmount, MySqlConnection conn, MySqlTransaction trans, AllowNegative allowNegative = AllowNegative.NotAllow)
        {
            var type = typePosition + ((int)transType * 1000) + tpId;
            Enum.TryParse(tpId.ToString(), out Tp tp);

            if (tpId == 0 && transType == TransType.Prize)
            {
                type = (int)FinancialType.Bonusreward;
            }

            var p = new DynamicParameters(new { CustId = custId, SiteId = siteId, UserName = userName, Amount = Amount, Type = type, Memo = $"{tp} {Memo}", RefTransId = RefTransId });
            p.Add("Is1stDepoist", dbType: DbType.Boolean, direction: ParameterDirection.Output);
            p.Add("AfterAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            if (allowNegative == AllowNegative.Allow)
            {
                p.Add("AllowNegative", (int)allowNegative, dbType: DbType.Decimal, direction: ParameterDirection.Input);
                _mainDbClient.Execute("Service_SeamlessWallet_Set_V2", p, conn, trans);
            }
            else
            {
                _mainDbClient.Execute("Wallet_Set_V2", p, conn, trans);
            }

            _ = p.Get<byte?>("Is1stDepoist") ?? 0;
            afterAmount = p.Get<decimal>("AfterAmount");

            if (allowNegative == AllowNegative.Allow)
            {
                return 1;
            }
            else
            {
                return afterAmount >= 0 ? 1 : 0;
            }
        }

        public decimal GetBalance(int custId)
        {
            return _reportDbClient.ExecuteScalar<decimal>("Balance_Get", new { CustId = custId });
        }
        
        public IEnumerable<BonusFulfill> GetBonusFulfill()
        {
            return _mainDbClient.Query<BonusFulfill>("Service_BonusFulfill_Get_V2", commandType: CommandType.StoredProcedure);
        }
        public int BonusSettle(BonusFulfill bonusFulfill)
        {
            try
            {
                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();
            
                var checkWalletType = CheckWalletType(bonusFulfill);
        
                var p = new DynamicParameters(bonusFulfill);
                p.Add("Bonus", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                if (checkWalletType.Type == 2)
                {
                    p.Add("TpId", dbType: DbType.Int32, direction: ParameterDirection.Output);
                }
                var result = _mainDbClient.Execute(checkWalletType.SP, p, conn, trans);
            
                if (result > 0) 
                {
                    //1 : 單一
                    //2 : 轉帳
                    //3 : SBSW
                    var bonus = p.Get<decimal>("Bonus");
                    switch (checkWalletType.Type)
                    {
                        case 1:
                            if (bonus > 0)
                            {
                                result = SetBalance(bonusFulfill.CustId, bonusFulfill.SiteId, bonusFulfill.UserName, bonus, 0,
                                    TransType.Prize, $"Bonus Transfer to Cash: {Math.Abs(bonus):#,##0.##}", 
                                    bonusFulfill.BonusId.ToString(), out _, conn, trans);
                            }
                            break;
                        case 2:
                            // 轉帳除了SBSW之外就無需退回主錢包
                            break;
                        case 3:
                            var parameters = SBSW_Refund(bonusFulfill, p.Get<decimal>("Bonus"));
                            result = _mainDbClient.Execute("Service_SeamlessWallet_Set_V2", parameters, trans);
                            _logger.LogInformation($"SBSW Bonus Bonus Fulfill Transfer to Cash:{JsonConvert.SerializeObject(parameters)} | result:{result} | bonus:{bonus}");
                            break;
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

        private WalletType CheckWalletType(BonusFulfill bff)
        {
            return bff.TpId switch
            {
                0 => new WalletType() { Type = 1, SP = "Service_BonusSettle_Set_V2", Memo = "Seamless" },
                > 0 when bff.PromotionCategoryId == 9 => new WalletType()
                {
                    Type = 3, SP = "Service_BonusSettle_Set_V3", Memo = "SBSW"
                },
                > 0 => new WalletType() { Type = 2, SP = "Service_BonusSettle_Set_V3", Memo = "Transfer" },
                _ => null
            };
        }

        private DynamicParameters SBSW_Refund(BonusFulfill bonusFulfill, decimal bonus)
        {
            var input = new
            {
                CustId = bonusFulfill.CustId,
                UserName = bonusFulfill.UserName,
                SiteId = bonusFulfill.SiteId,
                Amount = bonus,
                Type = 5,
                Memo = $"Bonus Transfer to Cash: {Math.Abs(bonus):#,##0.##}",
                RefTransId = bonusFulfill.BonusId
            };

            var allowNegative = 1;
            var parameters = new DynamicParameters(input);
            parameters.Add("AllowNegative", allowNegative, dbType: DbType.Decimal, direction: ParameterDirection.Input);
            parameters.Add("AfterAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            return parameters;
        }

        private class WalletType
        {
            public int Type { get; set; }
            public string SP { get; set; }
            public string Memo { get; set; }
        }
    }

}
