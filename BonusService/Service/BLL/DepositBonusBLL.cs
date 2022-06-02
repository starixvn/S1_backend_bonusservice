using BonusService.Service.DAL;
using BonusService.Service.Model;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using BonusService.Service.Library;
using BonusService.Service.Model.enums;
using Microsoft.Extensions.Logging;

namespace BonusService.Service.BLL
{
    public class DepositBonusBLL
    {
        private readonly DepositBonusService depositBonusService;
        private readonly string apiUrl;
        private readonly CourierAdapter courierAdapter;
        private readonly BonusAPIBLL bonusAPIBLL;
        private readonly BonusAPIServiceV2 bonusAPIServiceV2;
        private readonly ILogger<DepositBonusBLL> _logger;

        public DepositBonusBLL(DepositBonusService depositBonusService, IConfiguration configuration, CourierAdapter courierAdapter, BonusAPIBLL bonusAPIBLL, BonusAPIServiceV2 bonusAPIServiceV2, ILogger<DepositBonusBLL> logger)
        {
            this.depositBonusService = depositBonusService;
            this.apiUrl = configuration.GetValue<string>("TpService");
            this.courierAdapter = courierAdapter;
            this.bonusAPIBLL = bonusAPIBLL;
            this.bonusAPIServiceV2 = bonusAPIServiceV2;
            _logger = logger;
        }
        public IEnumerable<DepositBonus> GetDepositBonusQueue()
        {
            return depositBonusService.GetDepositBonusQueue();
        }
        public IEnumerable<DepositBonusCustList> GetDepositBonusQueueCustList()
        {
            return depositBonusService.GetDepositBonusQueueCustList();
        }
        public IEnumerable<DepositBonus> GetDepositBonusQueueByCustId(int custId, bool useMainDb = false)
        {
                return bonusAPIServiceV2.GetDepositBonusQueueByCustId(custId, useMainDb);
        }

        public void CheckDepositBonusExpired()
        {
            depositBonusService.CheckDepositBonusExpired();
        }
        public string GetDepositBonusQueueStatusComment(int status)
        {
            var Comment = "UnKnowStatus";
            switch (status)
            {
                case (int)DepositBonusQueueStatus.Undo:
                    Comment = "Undo";
                    break;
                case (int)DepositBonusQueueStatus.Finish:
                    Comment = "Bonus Dispatched";
                    break;
                case (int)DepositBonusQueueStatus.Expiry:
                    Comment = "DepositBonusQueue Expiry";
                    break;
                case (int)DepositBonusQueueStatus.Cancel_other_Bonus:
                    Comment = "Cancel-Already received other bonus";
                    break;
                case (int)DepositBonusQueueStatus.Cancel_not_eligible:
                    Comment = "Cancel-User is not eligible";
                    break;
                case (int)DepositBonusQueueStatus.Cancel_Rejected:
                    Comment = "Cancel-Deposit has been rejected";
                    break;

            }
            return Comment;
        }
        public int SetDepositBonusStatus(string transId, int status)
        {
            _logger.LogInformation($"Change DepositBonus Queue status TransId: {transId} status:{status} {GetDepositBonusQueueStatusComment(status)}");

            return depositBonusService.SetDepositBonusStatus( transId, status);
        }
        public int SetDepositBonusFinish(string transId, int custId)
        {
            var BonusList = GetDepositBonusQueueByCustId(custId,true); //改變狀態用mainDb取得資料，避免資料不同步
            foreach (var depositBonus in BonusList)
            {
                if (depositBonus.TransId == transId)
                {
                    SetDepositBonusStatus(depositBonus.TransId, (int)DepositBonusQueueStatus.Finish);
                }else
                {
                    SetDepositBonusStatus(depositBonus.TransId, (int)DepositBonusQueueStatus.Cancel_other_Bonus);
                }

            }
            return 1;
        }

    }
}
