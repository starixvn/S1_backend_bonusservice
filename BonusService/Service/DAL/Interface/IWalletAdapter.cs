using MySql.Data.MySqlClient;

namespace BonusService.Service.DAL.Interface
{
    public interface IWalletAdapter
    {
        int SetBalance(int custId, int siteId, string userName, decimal amount, int type, string memo, string refTransId, out decimal afterAmount, out byte is1StDeposit, MySqlTransaction trans);
    }
}