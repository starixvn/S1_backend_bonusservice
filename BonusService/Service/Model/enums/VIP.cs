namespace BonusService.Service.Model.enums
{
    public enum VipReviewStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3
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