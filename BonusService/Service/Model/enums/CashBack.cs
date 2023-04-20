namespace BonusService.Service.Model.enums
{
    public enum PromotionType
    {
        FstDeposit = 1,
        ReDeposit = 2,
        FreeBet = 3,
        Rebate = 6,
        Cashback = 7,
        SpecialBonus = 8,
        RetentionFreeBet = 9,
        DeviceBonus = 10
    }

    public enum PromotionCategoryId
    {
        SlotsGame = 1,
        LiveCasino = 2,
        SabaSports = 4,
        Lottery = 5
    }

    public enum TpCategoryId
    {
        Sports = 1,
        Casino = 2,
        LiveCasino = 3,
        Fishing = 4,
        Cockfighting = 5,
        Lottery = 6,
        VirtualSports = 7,
        Poker = 8
    }

    public enum FullFillType
    {
        RollOver = 1,
        WinOver = 2
    }
}