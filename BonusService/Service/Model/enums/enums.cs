namespace BonusService.Service.Model.enums
{
    public enum ErrorCode
    {
        Success = 0
    }

    public enum SettlementType
    {
        UnSettled = 0,
        SettledForValid = 1,/**/
        SettledForInvalid = 2,
        SettledForValidNotRollover = 3,/*TpId=1*/
        Cheating = 4
    }
    public enum TicketStatus
    {
        UnSettled = 0,/*Running, Waiting*/
        Win = 1,/*----*/
        Lost = 2,/*----*/
        Refund = 3,/*Reject, Refund, Void, Draw*/
        Draw = 4,/*----*/
        Related = 5,
        FreeBetWin = 6
    }

    public enum GameType
    {
        OW_Sports = 1,
        OW_NumberGame = 2,
        OW_VirtualSport = 3,
        OW_RNG = 4,
        OW_Keno = 5,
        OW_Allbet = 6,
        PP_RNG = 7,
        AG_LiveCasino = 8,
        SA_LiveCasino = 9,
        DG_LiveCasino = 10,
        GPI_RNG = 11,
        YL_Fish = 12,
        Joker_RNG = 13,
        YGG_RNG = 14,
        SG_RNG = 15,
        PP_LiveCasino = 16
    }
    public enum Tp
    {
        None = 0,
        OW = 1,
        SA = 2,
        AG = 3,
        DG = 4,
        PP = 5,
        GPI = 6,
        YL = 7,
        YGG = 8,
        Joker = 9,
        SG = 10
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
    
    public enum FinancialType
    {
        CancelledBonus = 7
    }
    public enum BonusInfoAtPage
    {
        Promotion = 0,
        Deposit = 1
    }
}
