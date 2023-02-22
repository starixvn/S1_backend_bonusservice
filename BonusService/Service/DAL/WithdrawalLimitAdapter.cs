using BonusService.Service.DAL.Interface;
using MySql.Data.MySqlClient;

namespace BonusService.Service.DAL
{
    public class WithdrawalLimitAdapter : IWithdrawalLimitAdapter
    {
        private readonly WithdrawalLimitService _withdrawalLimitService;

        public WithdrawalLimitAdapter(WithdrawalLimitService withdrawalLimitService)
        {
            _withdrawalLimitService = withdrawalLimitService;
        }

        public int SetWithdrawalLimit(int custId, string userName, int siteId, decimal withdrawalLimit, string memo, string refTransId , int createFrom, string creator ,out decimal afterWithdrawalLimit, MySqlTransaction trans)
        {
            return _withdrawalLimitService.SetWithdrawalLimit(custId, userName, siteId, withdrawalLimit, memo, refTransId, createFrom , creator , out afterWithdrawalLimit, trans);
        }
    }
}