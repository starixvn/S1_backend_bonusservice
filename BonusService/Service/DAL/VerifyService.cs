using BonusService.Service.Model.enums;
using BonusService.Service.Model;
using Dapper;
using MySqlX.XDevAPI.Common;
using Newtonsoft.Json;
using BonusService.Service.Library;
using BonusService.Service.DAL.Interface;

namespace BonusService.Service.DAL
{
    public class VerifyService : IVerifyService
    {
        private readonly MySqlClient _reportDbClient;
        public VerifyService(IServiceProvider provider)
        {
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        /// <summary>
        /// 驗證是否為相同姓名
        /// </summary>
        /// <param name="custInfo">受惠會員資訊(欲拿或領取獎勵的人)</param>
        /// <param name="custIds">需比較的會員清單</param>
        /// <returns></returns>
        public bool IsRealNameVaild(Customer custInfo, IEnumerable<int> custIds, out List<CompareRealNameDto> realNameCheckResult)
        {
            var compareNameInfo = new List<CompareRealNameDto>();
            var p2 = new DynamicParameters(new
            {
                _CustIds = JsonConvert.SerializeObject(custIds),
            });
            var setRealNameLog = _reportDbClient.Query<CustSetRealNameLogDto>("Service_SetRealNameLog_Get", p2).ToList();
            AddBeforeNameWithAfterNameToCompareList(compareNameInfo, setRealNameLog); //有改過名的清單

            var patterns = new List<string>
            {
                custInfo.AccountName,
                custInfo.AccountName.Replace(" ", string.Empty)
            };

            // 檢查是否有相同
            realNameCheckResult = new List<CompareRealNameDto>();
            foreach (var name in patterns)
            {
                realNameCheckResult.AddRange(compareNameInfo.Where(r => r.RealName.Equals(name, StringComparison.CurrentCultureIgnoreCase)).ToList());
            }

            return realNameCheckResult.Any() ? true : false;
        }

        /// <summary>
        /// 已領取相同 Bonus "且" 有改過名的玩家清單
        /// </summary>
        /// <param name="compareNameInfo"></param>
        /// <param name="setRealNameLog"></param>
        private static void AddBeforeNameWithAfterNameToCompareList(List<CompareRealNameDto> compareNameInfo, List<CustSetRealNameLogDto> setRealNameLog)
        {
            compareNameInfo.AddRange(setRealNameLog.Select(r => new CompareRealNameDto()
            {
                CustId = r.CustId,
                UserName = r.UserName,
                RealName = r.BeforeRealName
            }));

            compareNameInfo.AddRange(setRealNameLog.Select(r => new CompareRealNameDto()
            {
                CustId = r.CustId,
                UserName = r.UserName,
                RealName = r.AfterRealName
            }));
        }

    }
}
