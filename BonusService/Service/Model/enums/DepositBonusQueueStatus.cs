namespace BonusService.Service.Model.enums
{
    public enum DepositBonusQueueStatus
    {
        Undo = 1,
        Finish = 2,
        Expiry = 3,
        Cancel_other_Bonus = 4,//Rewarded other Bonus
        Cancel_not_eligible = 5,
        Cancel_Rejected = 6
    }

}
