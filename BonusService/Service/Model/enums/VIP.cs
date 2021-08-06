namespace BonusService.Service.Model.enums
{
    public enum VipReviewStatus
    {
        Pending = 1, //已暫停
        Approved = 2, //已核准
        Rejected = 3, //已拒絕
        Settled = 4 //已異動
    }

    public enum TaskType
    {
        UpgradeReview = 1,
        UpgradeCustomer = 2,
        DowngradeReview = 3,
        DowngradeCustomer = 4
    }

    public enum VipTypeEnum
    {
        Upgrade = 1,
        Downgrade = 2
    }
}