using BonusService.Service.BLL;
using BonusService.Service.DAL.Interface;
using BonusService.Service.Extensions;
using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;

using Dapper;

using Newtonsoft.Json;

namespace BonusService.Service.DAL
{
    public class VerifyService : IVerifyService
    {
        private readonly MySqlClient _reportDbClient;
        private readonly ILogger<VerifyService> _logger;
        public VerifyService(IServiceProvider provider, ILogger<VerifyService> logger)
        {
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _logger = logger;
        }

        /// <summary>
        /// 驗證是否為相同姓名
        /// </summary>
        /// <param name="custInfo">受惠會員資訊(欲拿或領取獎勵的人)</param>
        /// <param name="custIds">需比較的會員清單</param>
        /// <returns></returns>
        public bool IsRealNameVaild(Customer mainCustInfo, IEnumerable<CompareRealNameDto> compareCustIds, out List<CompareRealNameDto> realNameCheckResult)
        {
            #region Sub-需要比較的客戶清單：包含現在使用的AccountName和以前修改過的AccountName
            _logger.LogInformation($"mainCustInfo={mainCustInfo}, compareCustIds={JsonConvert.SerializeObject(compareCustIds.Select(x => x.CustId))}");
            realNameCheckResult = new List<CompareRealNameDto>();
            try
            {
                if (string.IsNullOrWhiteSpace(mainCustInfo.AccountName))
                {
                    return false; //mainCust沒有AccountName=> 沒有相同的清單
                }
                var compareNameInfo = new List<CompareRealNameDto>();
                compareNameInfo.AddRange(compareCustIds); //現在使用AccountName的清單
                var p = new DynamicParameters(new
                {
                    _CustIds = JsonConvert.SerializeObject(compareCustIds.Select(x => x.CustId)),
                });
                var setRealNameLog = _reportDbClient.Query<CustSetRealNameLogDto>("Service_SetRealNameLog_Get", p).ToList();
                AddBeforeNameWithAfterNameToCompareList(compareNameInfo, setRealNameLog); //過去有改過名的清單
                compareNameInfo = compareNameInfo.Distinct().ToList();

                #endregion

                #region Main-需要比較的客戶清單：包含現在使用的AccountName和以前修改過的AccountName

                var patterns = new List<string> //現在使用AccountName的清單
            {
                mainCustInfo.AccountName,
                mainCustInfo.AccountName.Replace(" ", string.Empty)
            };

                var p2 = new DynamicParameters(new
                {
                    _CustIds = JsonConvert.SerializeObject(new List<int>() { mainCustInfo.CustId }),
                });

                var mainCustIdSetRealNameLog = _reportDbClient.Query<CustSetRealNameLogDto>("Service_SetRealNameLog_Get", p2).ToList();
                AddBeforeNameWithAfterNameToPatternList(patterns, mainCustIdSetRealNameLog); //過去有改過名的清單
                patterns = patterns.Distinct().ToList();

                #endregion

                #region 檢查邏輯：MainCust所有使用(過)的AccountName(patterns)，與SubCust紀錄比較是否有相同
                foreach (var name in patterns)
                {
                    realNameCheckResult.AddRange(compareNameInfo.Where(r => r.RealName.Equals(name, StringComparison.CurrentCultureIgnoreCase)).ToList());
                }

                #endregion

                //true=有相同的清單，false=沒有相同的清單
                return realNameCheckResult.Any() ? true : false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MethodExtension.GetCurrentMethod());
                throw ex;
            }
        }

        /// <summary>
        /// 已領取相同 Bonus "且" 有改過名的玩家清單
        /// </summary>
        /// <param name="compareNameInfo"></param>
        /// <param name="setRealNameLog"></param>
        private static void AddBeforeNameWithAfterNameToPatternList(List<string> patterns, List<CustSetRealNameLogDto> mainCustIdSetRealNameLog)
        {
            patterns.AddRange(mainCustIdSetRealNameLog.Select(r => r.BeforeRealName));
            patterns.AddRange(mainCustIdSetRealNameLog.Select(r => r.BeforeRealName.Replace(" ", string.Empty)));
            patterns.AddRange(mainCustIdSetRealNameLog.Select(r => r.AfterRealName));
            patterns.AddRange(mainCustIdSetRealNameLog.Select(r => r.AfterRealName.Replace(" ", string.Empty)));
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
                RealName = r.BeforeRealName.Replace(" ", string.Empty)
            }));

            compareNameInfo.AddRange(setRealNameLog.Select(r => new CompareRealNameDto()
            {
                CustId = r.CustId,
                UserName = r.UserName,
                RealName = r.AfterRealName
            }));

            compareNameInfo.AddRange(setRealNameLog.Select(r => new CompareRealNameDto()
            {
                CustId = r.CustId,
                UserName = r.UserName,
                RealName = r.AfterRealName.Replace(" ", string.Empty)
            }));
        }

    }
}
