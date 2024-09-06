namespace BonusService.Service.Model.enums
{
    public enum BonusErrorCode
    {
        Error = 0,
        Success = 1,
        //API error -10xx
        PromotionNotFound = -1001,
        IpIsBlocked = -1002,
        ProfileOrPhoneNotFinish = -1003,
        DepositAmountNotEnough = -1004,
        MainWalletAmountNotEnough = -1005,
        CalculateAmountIsNegative = -1006,
        CalculateCashbackTotalBonusLessThenMinPay = -1007,
        CalculateUnknownPromotionType = -1008,
        DuplicateClaimBonus = -1009,
        TierAmountNoFound = -1010,//未找到對應的級距設定
        SameIp = -1011,
        SameRealName = -1012,
        SameDeviceId = -1013,
        BlockTag = -1014,


        //SP Return ERROR -11XX
        SpBonusNotFound = -1101,
        SpCustomerNotJoinSpecialCode = -1102,
        SpCustomerJoinThisPromotionBefore = -1103,
        SpCustomerBonusStillRunning = -1104,
        SpCustomerDepositNotExceedRequiredAmount = -1105,
        SpCustomerDontHaveFirstDeposit = -1106,
        SpCustomerFirstDepositRuleDontMatch = -1107,
        SpCustomerWithdrawalBefore = -1108,
        SpCustomerAddOtherBonusInPeriodTime = -1109,
        SpCustomerBonusStillRunningOrAddOtherBonusWhichPromotionTypeIn135 = -1110,
        SpCustomerDepositApproveBefore = -1111,
        SpCustomerBonusStillRunningOrAddThisBonusBefore = -1112,
        SpCustomerJoinThisRebateBefore = -1113,
        SpCustomerHaveRunningBonusOrJoinThisBonusInPeriodTime = -1114,
        SpUndefinedPromotionType = -1115,
        SpCustomerNotHaveNewScratchCardToJoin = -1116,

        //AdjustmentBonus Error -12XX
        AdjustmentBonusPromotionTypeNotMatch = -1202,
        AdjustmentBonusPromotionNotActive = -1203,
        AdjustmentBonusPromotionAmountOverMaxBonus = -1204,
        AdjustmentBonusCustomerHasRunningBonus = -1205,
        UnknownError = -9999

    }
}
