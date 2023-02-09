namespace BonusService.Service.Model.enums
{
    public enum ErrorCode
    {
        Success = 0
    }
    
    public enum Tp
    {
        None = 0,
        Saba = 1,
        SA = 2,
        AG = 3,
        DG = 4,
        PP = 5,
        GPI = 6,
        YL = 7,
        YGG = 8,
        Joker = 9,
        SG = 10,
        KM = 11,
        Allbet = 12,
        OneGaming = 13,
        PG = 14,
        Sexy = 15,
        CQ9 = 16,
        CF = 17,
        EBet = 18,
        AWC = 19,
        WM = 20,
        Kiss918 = 21,
        Mega = 22,
        One789 = 23,
        Iconic = 24,
        V8 = 25,
        PT = 27,
        N2Live = 28,
        BTI = 29,
        AMB = 30,
        NineW = 31,
        JILI = 32,
        TCG = 33,
        MG = 34,
        MP = 35
    }

    public enum VipPeriodType
    {
        Daily = 1,
        Weekly = 2,
        Monthly = 3
    }

    public enum VipPreviousDateRangeType
    {
        LastMonth = 1,
        LastThreeMonth = 2
    }
    
    public enum BonusType
    {
        Percentage = 1,
        Amount = 2
    }

    public enum PromotionDeviceType
    {
        All = 1,
        Web = 2,
        App = 3
    }
    
    public enum LockedType
    {
        FromBonus = 1,
        FromRequireAmount = 2
    }
    public class CustRollOver
    {
        public decimal RollOver { get; set; }
        public decimal TurnOver { get; set; }
    }
    
    public enum DataBase
    {
        Main, //ConnectionString
        Report,//ConnectionStringReport
        Wager,//ConnectionStringWager
        Log,//ConnectionStringLog
    }
    
    public enum RedisDataBase
    {
        Member = 0,
        Shared = 1,
        Tp = 2,
        Bonus = 3,
        Admin = 4,
        Payment = 5
    }
    
    public enum FinancialType
    {
        LocalBankDeposit = 1,
        PaymentAdjustmentDebit = 2,
        PaymentAdjustmentCredit = 3,
        Bonusreward = 5,
        LockedAmount = 6,
        CancelledBonus = 7,
        Cashreward = 8,
        ReferFriendsRewards = 13,
        VerificationReward = 20
    }
    public enum WalletStatus
    {
        Normal = 1,
        UM = 2,
        Error = 3,
    }
    public enum SiteId
    {
        Nova88 = 1,
        Goal123 = 2,
        Bwing = 3
    }
    public enum DepositBonusQueueStatus : int
    {
        Undo = 1,
        Finish = 2,
        Expiry = 3,
        Cancel_other_Bonus = 4,//Rewarded other Bonus
        Cancel_not_eligible = 5,
        Cancel_Rejected = 6
    }
    public enum AdjustStatus
    {
        Draft = 0,
        Pending = 1,
        Approved = 2,
        Rejected = 3,
        OnHold = 4,
        Error = 99
    }
    public enum BonusInfoAtPage
    {
        Promotion = 0,
        Deposit = 1
    }
    public enum OTPVerificationType
    {
        
        SMS = 0,
        Email = 1,
        All = 2,
    }
    public enum OTPTypeEnum
    {
        SMS = 0,
        Email = 1
    }
    public enum DepositStatusEnum
    {
        Draft = 0,    
        Pending = 1,
        Paid = 2,     
        Approved = 3,
        Rejected = 4,
        Canceled = 5,
        Expired = 6,  
        OnHold = 7,   
        Error = 99    
    }

    public enum AllowIpType
    {
        DormantAccountLock = 1,
        Bonus = 2
    }

    /// <summary>
    /// <remarks>
    /// Use Table：RebateCLog,RebateMLog
    /// </remarks>
    public enum RebateType
    {
        VipRebate = 1,
    }
    
    public enum SpecialCode
    {
        Success = 0,
        SpecialCodeError = 99,
        SpecialCodeErrorForCheck = 1000,
        SpecialCodeVipLevelsOrBlockTagInvalid = 1001, // VipLevel 不符合 或是有 BlockTag
        PromoCodeHasBeenUsed = 3000
    }
}
