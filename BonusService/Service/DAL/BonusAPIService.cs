using BonusService.Service.Library;
using BonusService.Service.Model.enums;

using Dapper;

using MemberSite.Service.Model;

using MySql.Data.MySqlClient;

namespace BonusService.Service.DAL
{
    public class BonusAPIService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly WalletService _walletService;
        private readonly WithdrawalLimitService _withdrawalLimitService;

        public BonusAPIService(IServiceProvider serviceProvider)
        {
            _walletService = serviceProvider.GetRequiredService<WalletService>();
            _withdrawalLimitService = serviceProvider.GetRequiredService<WithdrawalLimitService>();
            _mainDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public VerificationRewardData GetVerificationReward(int siteId, int currencyId)
        {
            var p = new DynamicParameters(new { _SiteId = siteId, _CurrencyId = currencyId });
            return _reportDbClient.QueryFirst<VerificationRewardData>("Service_VerificationReward_Get", p);
        }

        public VerificationRewardLimit GetVerificationRewardLimit(int siteId, int currencyId, OTPTypeEnum type)
        {
            var p = new DynamicParameters(new { _SiteId = siteId, _CurrencyId = currencyId, _Type = type });
            return _reportDbClient.QueryFirst<VerificationRewardLimit>("Service_VerificationRewardLimit_Get", p);
        }

        public DateTime? GetVerifyTime(int custId, OTPTypeEnum type)
        {
            var taskId = (int)type + 3;
            var p = new DynamicParameters(new { _CustId = custId, _TaskId = taskId });
            return _mainDbClient.QueryFirst<DateTime?>("Service_CustVerifyTime_Get", p);
        }

        public CustVerificationRewardData GetCustVerificationReward(int custId, bool usedMainDb = false)
        {
            var p = new DynamicParameters(new { _CustId = custId });
            if (usedMainDb)
                return _mainDbClient.QueryFirst<CustVerificationRewardData>("Service_CustVerificationReward_Get_V2", p);
            else
                return _reportDbClient.QueryFirst<CustVerificationRewardData>("Service_CustVerificationReward_Get_V2", p);

        }

        private int SetCustVerifiedReward(int custId, int siteId, int currencyId, int verifyItem, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new { _CustId = custId, _SiteId = siteId, _CurrencyId = currencyId, _VerificationItem = verifyItem });
            return _mainDbClient.Execute("Service_CustVerificationReward_Set", trans, p);
        }

        public ApiResult PayoutVerificationReward(int custId, int siteId, int currencyId, string userName, OTPVerificationType OTPVerificationtype, decimal reward)
        {
            var apiResult = new ApiResult();
            try
            {
                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();

                var type = 20;
                var transId = "";
                string comment;
                if (OTPVerificationtype == OTPVerificationType.Email)
                {
                    comment = $"email verification reward: {reward:#,##0.##}";
                }
                else if (OTPVerificationtype == OTPVerificationType.SMS)
                {
                    comment = $"phone verification reward: {reward:#,##0.##}";
                }
                else
                {
                    comment = $"verification reward: {reward:#,##0.##}";
                }


                var result = _walletService.SetBalance(custId, siteId, userName, reward, type,
                    comment, transId, out _, out _, trans);
                if (result <= 0)
                {
                    apiResult.Message = "SetBalance fail";
                    return apiResult;
                }
                result = _withdrawalLimitService.SetWithdrawalLimit(custId, userName, siteId, reward, 0, comment, transId, (int)WithdrawalLimitLogCreateFromEnum.VerificationReward, "System", out _, trans);
                if (result <= 0)
                {
                    apiResult.Message = "SetWithdrawalLimit fail";
                    return apiResult;
                }

                result = SetCustVerifiedReward(custId, siteId, currencyId, (int)OTPVerificationtype, trans);
                if (result <= 0)
                {
                    apiResult.Message = "SetCustVerifiedReward fail";
                    return apiResult;
                }

                _mainDbClient.CommitAndDisposeConnection(conn, trans);

                if (result > 0)
                {
                    apiResult.ErrorCode = 0;
                    apiResult.Message = comment;
                }
                else
                {
                    apiResult.Message = "UnKnow Fail";
                }
            }
            catch (Exception ex)
            {
                apiResult.ErrorCode = 999;
                apiResult.RawMessage = ex.ToString();
            }

            return apiResult;
        }
    }
}
