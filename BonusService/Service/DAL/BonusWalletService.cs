using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;

using Dapper;

using MySql.Data.MySqlClient;

namespace BonusService.Service.DAL
{
    public class BonusWalletService
    {

        private readonly MySqlClient _mainDbClient;

        public BonusWalletService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public bool RewardBonus(int bonusId, int promoCategoryId, int tpId, int custId, int siteId, string userName, decimal bonusAmount, decimal lockedAmount, decimal rollover, long expiryTime, int type, string Memo, string RefTransId, MySqlTransaction trans, int fullFillType = 1)
        {
            var p = new DynamicParameters(new
            {
                BonusId = bonusId,
                PromoCategoryId = promoCategoryId,
                TpId = tpId,
                CustId = custId,
                SiteId = siteId,
                UserName = userName,
                BonusAmount = bonusAmount,
                LockedAmount = lockedAmount,
                Rollover = rollover,
                ExpiryTime = expiryTime,
                Type = type,
                FullFillType = fullFillType,
                Memo,
                RefTransId
            });
            return _mainDbClient.Execute("Common_Bonus_Ins_V2", trans, p) > 0;
        }

        public int RewardCashBack(CustCashBackInput custCashBackInput, MySqlTransaction trans)
        {
            var parameter = new DynamicParameters(new
            {
                custCashBackInput.CustId,
                custCashBackInput.BonusCode,
                custCashBackInput.PromoCategoryId,
                custCashBackInput.CalculateDate
            });
            return _mainDbClient.Execute("Member_CustCashBack_Set", trans, parameter);
        }
    }
}
