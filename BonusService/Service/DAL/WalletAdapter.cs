using BonusService.Service.DAL.Interface;
using MySql.Data.MySqlClient;

namespace BonusService.Service.DAL
{
    public class WalletAdapter : IWalletAdapter
    {
        private readonly WalletService _wallet;

        public WalletAdapter(WalletService wallet)
        {
            _wallet = wallet;
        }

        public int SetBalance(int custId, int siteId, string userName, decimal amount, int type, string memo, string refTransId, out decimal afterAmount, out byte is1StDeposit, MySqlTransaction trans)
        {
            return _wallet.SetBalance(custId, siteId, userName, amount, type, memo, refTransId, out afterAmount, out is1StDeposit, trans);
        }
    }
}