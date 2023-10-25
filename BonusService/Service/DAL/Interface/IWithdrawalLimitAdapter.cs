using MySql.Data.MySqlClient;

namespace BonusService.Service.DAL.Interface
{
    public interface IWithdrawalLimitAdapter
    {
        int SetWithdrawalLimit(int custId, string userName, int siteId, decimal withdrawalLimit, string memo, string refTransId, int createFrom, string creator, out decimal afterWithdrawalLimit, MySqlTransaction trans);
    }
}