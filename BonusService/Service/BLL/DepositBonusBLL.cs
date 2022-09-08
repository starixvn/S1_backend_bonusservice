using BonusService.Service.DAL;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;

namespace BonusService.Service.BLL
{
    public class DepositBonusBLL
    {
        private readonly DepositBonusService depositBonusService;
        private readonly PromotionService _promotionService;
        private readonly ILogger<DepositBonusBLL> _logger;

        public DepositBonusBLL(DepositBonusService depositBonusService, PromotionService promotionService, ILogger<DepositBonusBLL> logger)
        {
            this.depositBonusService = depositBonusService;
            _promotionService = promotionService;
            _logger = logger;
        }

        public IEnumerable<DepositBonusCustList> GetDepositBonusQueueCustList()
        {
            return depositBonusService.GetDepositBonusQueueCustList();
        }
        public IEnumerable<DepositBonus> GetDepositBonusQueueByCustId(int custId, bool useMainDb)
        {
                return _promotionService.GetDepositBonusQueueByCustId(custId, useMainDb);
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
            var BonusList = GetDepositBonusQueueByCustId(custId, true); //改變狀態用mainDb取得資料，避免資料不同步
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
