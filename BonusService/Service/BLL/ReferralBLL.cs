using BonusService.Service.DAL;
using BonusService.Service.DAL.Interface;
using BonusService.Service.Model;
using Newtonsoft.Json;
using System;
using System.Text;

namespace BonusService.Service.BLL
{
    public class ReferralBLL
    {
        private readonly SelLangBLL selLangBLL;
        private readonly MessageService messageService;
        private readonly CustInfoService custInfoService;
        private readonly ReferralReward _referralReward;
        private readonly IReferralService _referralService;

        public ReferralBLL(SelLangBLL selLangBLL,
            MessageService messageService,
            CustInfoService custInfoService,
            ReferralReward referralReward,
            IReferralService referralService)
        {
            this.selLangBLL = selLangBLL;
            this.messageService = messageService;
            this.custInfoService = custInfoService;
            _referralReward = referralReward;
            _referralService = referralService;
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
                var custInfo = custInfoService.GetCustInfo(agentId);

                var msg = new StringBuilder();

                var dpayAmount = payAmount.ToString("#,##0.##");
                if (custInfoService.IsConvertRequired(custInfo.CurrencyId))
                {
                    dpayAmount = $"{payAmount / 1000:#,##0.##}";
                }

                var title = selLangBLL.GetResourceValue("lbl_ReferralBonusCreditedTitle", custInfo.UserLang);
                var message = string.Format($" {selLangBLL.GetResourceValue("lbl_ReferralBonusCreditedMsg", custInfo.UserLang)}", dpayAmount);
                msg.AppendLine(message);


                foreach (var paidReferral in paidReferralList)
                {
                    var referralFreeCredit = paidReferral.ReferralFreeCredit;
                    var dAmount = paidReferral.BonusAmount;
                    if (custInfoService.IsConvertRequired(paidReferral.CurrencyId))
                    {
                        referralFreeCredit /= 1000;
                        dAmount /= 1000;
                    }
                    var referredMessage = string.Format($"{selLangBLL.GetResourceValue("lbl_ReferredBonusCreditedMsg", paidReferral.UserLang)}", $"{referralFreeCredit:#,##0.##}");
                    messageService.SendMessage(new Message("System", paidReferral.CustId, paidReferral.UserLang, title, referredMessage));
                    
                    msg.AppendLine($"{paidReferral.UserName}: {dAmount:#,##0.##}");
                }

                messageService.SendMessage(new Message("System", custInfo.CustId, custInfo.UserLang, title, msg.ToString()));
            }

            return result;
        }

        public IEnumerable<WorkerPeriodTask> VerifyTask(string workerName)
        {
            //檢查參數
            //0.檢查參數是否正確
            //2.檢查期間型別需檢查項目是否符合

            var TaskList = _referralService.GetTask();

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
                if (task.LastCurrencySettledTime != null)
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

    }
}
