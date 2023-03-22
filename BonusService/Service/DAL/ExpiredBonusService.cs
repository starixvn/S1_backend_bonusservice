using BonusService.Service.Model;
using Dapper;
using BonusService.Service.Library;
using BonusService.Service.Model.enums;

namespace BonusService.Service.DAL
{
    public class ExpiredBonusService
    {
        private readonly WalletService _walletService;
        private readonly WithdrawalLimitService _withdrawalLimitService;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;

        public ExpiredBonusService(IServiceProvider provider)
        {
            _walletService = provider.GetRequiredService<WalletService>();
            _withdrawalLimitService = provider.GetRequiredService<WithdrawalLimitService>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }
        
        public IEnumerable<ExpiredBonus> GetBonusExpired(int limit)
        {
            return _reportDbClient.QueryUseCommandDefinition<ExpiredBonus>("Service_BonusExpired_Get", new DynamicParameters(new { Limit = limit }));
        }
        
        public IEnumerable<ExpiredBonus> GetTransferBonusExpired(int limit)
        {
            return _reportDbClient.QueryUseCommandDefinition<ExpiredBonus>("Service_TransferBonusExpired_Get", new DynamicParameters(new { Limit = limit }));
        }
        
        public IEnumerable<decimal> GetBonusWinLoss(int bonusId)
        {
            return _reportDbClient.Query<decimal>("Service_BonusWinLoss_Get", new DynamicParameters(new { BonusId = bonusId }));
        }
        
        public int RefundExpiredBonus(ExpiredBonus expiredBonus, string memo)
        {
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            var result = 0;
            if (expiredBonus.Refund > 0) //money
            {
                result = _walletService.SetBalance(expiredBonus.CustId, expiredBonus.SiteId, expiredBonus.UserName,
                    expiredBonus.Refund, (int)FinancialType.CancelledBonus, memo,
                    expiredBonus.BonusId.ToString(), out _, out _, trans);
                
                if (result >= 0)
                {
                    result = _withdrawalLimitService.SetWithdrawalLimit(expiredBonus.CustId, expiredBonus.UserName,
                        expiredBonus.SiteId, expiredBonus.Refund, 0 ,"CancelBonus", expiredBonus.BonusId.ToString(),
                        (int) WithdrawalLimitLogCreateFromEnum.Bonus, "System" , out _, trans);
                }
            }

            if (result >= 0)
            {
                result = _mainDbClient.Execute("Service_BonusExpired_Set_V2",trans,
                    new DynamicParameters(new
                    {
                        expiredBonus.BonusId,
                        expiredBonus.CustId,
                        expiredBonus.UserName,
                        expiredBonus.SiteId,
                        TYPE = (int)FinancialType.CancelledBonus,
                        expiredBonus.PromoCategoryId,
                        expiredBonus.Bonus,
                        Memo = "Expired Bonus" 
                    }));
            }
            
            if (result >= 0)
            {
                var cp = new DynamicParameters(
                    new
                    {
                        _BonusId = expiredBonus.BonusId,
                        _CancelBonus = expiredBonus.Bonus - expiredBonus.Refund
                    });
                result = _mainDbClient.Execute("Service_CancelBonusAmount_Set",trans,cp);
            }

            if (result >= 0)
            {
                trans.Commit();
            }
            
            conn.Close();
            conn.Dispose();
            return result;
        }
    }
}
