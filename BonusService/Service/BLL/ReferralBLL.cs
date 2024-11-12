using BonusService.Service.DAL;
using BonusService.Service.DAL.Interface;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using Newtonsoft.Json;
using System.Text;

namespace BonusService.Service.BLL
{
    public class ReferralBLL
    {
        private readonly SelLangBLL _selLangBll;
        private readonly MessageService _messageService;
        private readonly CustInfoService _custInfoService;
        private readonly ReferralReward _referralReward;
        private readonly IWorkerPeriodService _workerPeriodService;
        private readonly IReferralService _referralService;
        private readonly IVerifyService _verifyService;
        private readonly ILogger<ReferralBLL> _logger;

        public ReferralBLL(SelLangBLL selLangBll,
            MessageService messageService,
            CustInfoService custInfoService,
            ReferralReward referralReward,
            IWorkerPeriodService workerPeriodService,
            IReferralService referralService,
            IVerifyService verifyService,
            ILogger<ReferralBLL> logger)
        {
            _selLangBll = selLangBll;
            _messageService = messageService;
            _custInfoService = custInfoService;
            _referralReward = referralReward;
            _referralService = referralService;
            _workerPeriodService = workerPeriodService;
            _verifyService = verifyService;
            _logger = logger;
        }
        public IEnumerable<ReferralInfo> GetReferralSettlement()
        {
            return _referralReward.GetReferralSettlement();
        }

        public int PayoutReferral(int agentId, IEnumerable<ReferralInfo> referralInfos, out decimal payAmount, out List<ReferralInfo> paidReferralList)
        {
            var result = _referralReward.Payout(referralInfos, out payAmount, out paidReferralList);
            if (result > 0 && payAmount > 0)
            {
                var custInfo = _custInfoService.GetCustInfo(agentId);

                var msg = new StringBuilder();

                var dpayAmount = payAmount.ToString("#,##0.##");
                if (_custInfoService.IsConvertRequired(custInfo.CurrencyId))
                {
                    dpayAmount = $"{payAmount / 1000:#,##0.##}";
                }

                var title = _selLangBll.GetResourceValue("lbl_ReferralBonusCreditedTitle", custInfo.UserLang);
                var message = string.Format($" {_selLangBll.GetResourceValue("lbl_ReferralBonusCreditedMsg", custInfo.UserLang)}", dpayAmount);
                msg.AppendLine(message);


                foreach (var paidReferral in paidReferralList)
                {
                    var referralFreeCredit = paidReferral.ReferralFreeCredit;
                    var dAmount = paidReferral.BonusAmount;
                    if (_custInfoService.IsConvertRequired(paidReferral.CurrencyId))
                    {
                        referralFreeCredit /= 1000;
                        dAmount /= 1000;
                    }
                    var referredMessage = string.Format($"{_selLangBll.GetResourceValue("lbl_ReferredBonusCreditedMsg", paidReferral.UserLang)}", $"{referralFreeCredit:#,##0.##}");
                    _messageService.SendMessage(new Message("System", paidReferral.CustId, paidReferral.UserLang, title, referredMessage));

                    msg.AppendLine($"{paidReferral.UserName}: {dAmount:#,##0.##}");
                }

                _messageService.SendMessage(new Message("System", custInfo.CustId, custInfo.UserLang, title, msg.ToString()));
            }

            return result;
        }

        public IEnumerable<WorkerPeriodTask> VerifyTask(string workerName)
        {
            //檢查參數
            //0.檢查參數是否正確
            //2.檢查期間型別需檢查項目是否符合

            var TaskList = _workerPeriodService.GetTask(workerName);

            var doTaskList = new List<WorkerPeriodTask>();

            foreach (var task in TaskList)
            {
                //Step1：檢查小時是否符合
                //幣別時區小時 < 指定小時，不執行排程
                if (task.CurrencyNowTime.Hour < task.RedeemHours)
                {
                    _logger.LogWarning($"task.RedeemsHours Invaild. task value={JsonConvert.SerializeObject(task)}");
                    continue;
                }

                //Step2：檢查最近一次執行時間是否符合
                //LastCurrencySettledTime 是 Null，代表第一次執行排程
                //LastCurrencySettledTime.Date = CurrencyNowTime.Date，上次執行時間日期等於幣別時區時間日期，不可再執行(原邏輯)
                if (task.LastCurrencySettledTime != null && task.SpecificDate == new DateTime(1970, 1, 1))
                {
                    if (task.LastCurrencySettledTime.Date == task.CurrencyNowTime.Date)
                    {
                        _logger.LogWarning($"task.LastCurrencySettledTime Invaild. task value={JsonConvert.SerializeObject(task)}");
                        continue;
                    }
                }

                doTaskList.Add(task);
            }

            return doTaskList;
        }

        public IEnumerable<DepositReferralCustInfo> GetDepositReferralCustList(
            int siteId,
            int currencyId,
            DateTime specificDate,
            string currencyTimeZone)
            => _referralService.GetDepositReferralCustList(siteId, currencyId, specificDate, currencyTimeZone);

        public bool IsReferrerValid(Customer referrerInfo)
        {
            //推薦人是suspend狀態就不派發
            if (referrerInfo.Status == (int)CustomerStatusEnum.Suspend)
                return false;

            //推荐人的账号必须是有存款过的记录
            if (referrerInfo.ActStatus == (int)ActStatusEnum.NonDepoist)
                return false;

            return true;
        }

        public void FilterReferredDetail(Customer referrerInfo, IEnumerable<DepositReferralCustInfo> referredList,
            out IEnumerable<DepositReferralCustInfo> filterReferredList)
        {
            //被推薦人是suspend狀態就不派發
            referredList = referredList.Where(x => x.Referred_Status != (int)CustomerStatusEnum.Suspend);

            var referredCustList = referredList.Select(x => new CompareRealNameDto()
            {
                CustId = x.Referred_CustId,
                UserName = x.Referred_UserName,
                RealName = x.Referred_AccountName.Replace(" ", string.Empty)
            }).Distinct();

            //同姓名限制：推薦人跟被推薦人不可相同姓名(包含修改過的姓名)，如果同名，則該被推薦人資料不計算
            _verifyService.IsRealNameVaild(referrerInfo, referredCustList, out List<CompareRealNameDto> realNameCheckResult);
            if (realNameCheckResult.Any())
            {
                referredCustList = referredCustList.Where(x => !realNameCheckResult.Select(x => x.CustId).Contains(x.CustId));
                _logger.LogInformation($"Real Name Check={referrerInfo.SiteId}-{referrerInfo.CurrencyId}-{referrerInfo.CustId}-{referrerInfo.UserName} \n" +
                    $"Result={JsonConvert.SerializeObject(realNameCheckResult)}");
            }
            filterReferredList = referredList.Where(x => referredCustList.Select(x => x.CustId).Contains(x.Referred_CustId));
        }

        public DepositReferralReferrerCaculateResult CaculateDepositReferral(IEnumerable<DepositReferralCustInfo> custInfo)
        {
            var referredInfo = custInfo.GroupBy(x => x.Referred_CustId)
                .Select(group => new DepositReferralReferredInfo()
                {
                    Referred_CustId = group.Key,
                    Referred_UserName = group.First().Referred_UserName,
                    TotalDeposit = group.Sum(x => x.ActualDeposit),
                    DepositRewardPercentage = group.First().DepositRewardPercentage,
                    DepositInfo = group.ToDictionary(o => o.TransId, o => o.ActualDeposit)
                })
                .ToList();

            var firstCustInfo = custInfo.First();

            return new DepositReferralReferrerCaculateResult(
            firstCustInfo.SettledDate,
            firstCustInfo.Referrer_CustId,
            firstCustInfo.Referrer_UserName,
            firstCustInfo.MinPayout,
            firstCustInfo.MaxPayout,
            referredInfo.Sum(x => x.CaculateDepositReferral),
            referredInfo);
        }

        public int PayoutDepositReferral(Customer referrerInfo, DepositReferralReferrerCaculateResult caculateResult)
        {
            var result = _referralReward.DepositReferralPayout(referrerInfo.SiteId, referrerInfo.CurrencyId, caculateResult);
            if (result > 0 && caculateResult.RewardDepositReferral > 0)
            {
                var msg = new StringBuilder();

                var dpayAmount = caculateResult.RewardDepositReferral.ToString("#,##0.##");
                if (_custInfoService.IsConvertRequired(referrerInfo.CurrencyId))
                    dpayAmount = $"{caculateResult.RewardDepositReferral / 1000:#,##0.##}";

                var title = _selLangBll.GetResourceValue("lbl_ReferralBonusCreditedTitle", referrerInfo.UserLang);
                var message = string.Format($" {_selLangBll.GetResourceValue("lbl_ReferralBonusCreditedMsg", referrerInfo.UserLang)}", dpayAmount);
                msg.AppendLine(message);

                _messageService.SendMessage(new Message("System", referrerInfo.CustId, referrerInfo.UserLang, title, msg.ToString()));
            }

            return result;
        }


        public int SetTaskDone(int siteId, int currencyId, string workerName)
        {
            return _workerPeriodService.SetTaskDone(siteId, currencyId, workerName);
        }

    }
}
