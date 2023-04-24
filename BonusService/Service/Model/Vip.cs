namespace BonusService.Service.Model
{
    public class VipSystem
    {
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
        public int Level { get; set; }
        public decimal UpDeposit { get; set; }
        public decimal UpBonus { get; set; }
        public decimal? DownDeposit { get; set; }
        public decimal? DownTurnover { get; set; }
    }

    public class CustVip
    {
        public int CustId { get; set; }
        public int VipLevel { get; set; }
        public string UserName { get; set; }
        public decimal DepositAmount { get; set; }
    }

    public class CustVipForDowngrade
    {
        public int CustId { get; set; }
        public int VipLevel { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal TurnoverAmount { get; set; }
        public int Exclusive { get; set; }
    }


    public class VipSetting
    {
        public int SiteId { get; set; }
        public int Status { get; set; }
        public int PeriodType { get; set; }
        public int PeriodInterval { get; set; }
        public int PreviousDateRangeType { get; set; }
        public int WithdrawalLimitRatio { get; set; }
        public DateTime? SettleTime { get; set; }
        public DateTime? LastUpdateTime { get; set; }
        public int AssignReviewDate { get; set; }
        public DateTime? LastReviewedTime { get; set; }
        public int AssignSettleDate { get; set; }
        public DateTime? LastSettledTime { get; set; }
    }

    public class VipUpgradeReview
    {
        public int SiteId { get; set; }
        public int CustId { get; set; }
        public string UserName { get; set; }
        public string UserLang { get; set; }
        public int CurrencyId { get; set; }
        public int CurrentVipLevel { get; set; }
        public int UpgradeVipLevel { get; set; }
        public decimal UpgradeBonus { get; set; }
    }

    public class CustomerUpgradeInfo
    {
        public CustomerUpgradeInfo(int vipLevel, decimal upDeposit, int siteId, int currencyId, DateTime startTime,
            DateTime endTime, string calculateMonth)
        {
            VipLevel = vipLevel;
            UpDeposit = upDeposit;
            SiteId = siteId;
            CurrencyId = currencyId;
            StartTime = startTime;
            EndTime = endTime;
            CalculateMonth = calculateMonth;
        }

        public int VipLevel { get; }
        public decimal UpDeposit { get; }
        public int SiteId { get; }
        public int CurrencyId { get; }
        public DateTime StartTime { get; }
        public DateTime EndTime { get; }
        public string CalculateMonth { get; }
    }

    public class CustomerVipInfo
    {
        public CustomerVipInfo(int oldLevel, int newLevel, int custId, string userName, int siteId,
            decimal upgradeAmount, decimal withdrawalLimitAmount, string financialMemo, string withdrawalLimitMemo,
            string calculateMonth)
        {
            OldLevel = oldLevel;
            NewLevel = newLevel;
            CustId = custId;
            UserName = userName;
            SiteId = siteId;
            UpgradeAmount = upgradeAmount;
            WithdrawalLimitAmount = withdrawalLimitAmount;
            FinancialMemo = financialMemo;
            WithdrawalLimitMemo = withdrawalLimitMemo;
            CalculateMonth = calculateMonth;
        }

        /// <summary>
        /// 降級參數
        /// </summary>
        /// <param name="oldLevel">降級前等級</param>
        /// <param name="newLevel">降級後等級</param>
        /// <param name="custId">客戶ID</param>
        /// <param name="userName">客戶名稱</param>
        /// <param name="siteId">網站ID</param>
        /// <param name="calculateMonth">計算年月區間</param>
        public CustomerVipInfo(int oldLevel,
            int newLevel,
            int custId,
            string userName,
            int siteId,
            string calculateMonth)
        {
            OldLevel = oldLevel;
            NewLevel = newLevel;
            CustId = custId;
            UserName = userName;
            SiteId = siteId;
            CalculateMonth = calculateMonth;
        }

        public int OldLevel { get; }
        public int NewLevel { get; }
        public int CustId { get; }
        public string UserName { get; }
        public int SiteId { get; }
        public decimal UpgradeAmount { get; }
        public decimal WithdrawalLimitAmount { get; }
        public string FinancialMemo { get; }
        public string WithdrawalLimitMemo { get; }
        public string CalculateMonth { get; }
        public DateTime SettleTime { get; }
    }

    public class VipUpgradeInfo
    {
        public VipUpgradeInfo(string calculateMonth, int siteId, int status)
        {
            SiteId = siteId;
            CalculateMonth = calculateMonth;
            Status = status;
        }

        public VipUpgradeInfo(int custId, string calculateMonth, string userName, int siteId, int currentVipLevel,
            int upgradeVipLevel, int currencyId, decimal depositAmount, decimal upgradeAmount, int status)
        {
            CustId = custId;
            CalculateMonth = calculateMonth;
            UserName = userName;
            SiteId = siteId;
            CurrentVipLevel = currentVipLevel;
            UpgradeVipLevel = upgradeVipLevel;
            CurrencyId = currencyId;
            DepositAmount = depositAmount;
            UpgradeAmount = upgradeAmount;
            Status = status;
        }

        public int CustId { get; }
        public string CalculateMonth { get; }
        public string UserName { get; }
        public int SiteId { get; }
        public int CurrentVipLevel { get; }
        public int UpgradeVipLevel { get; }
        public int CurrencyId { get; }
        public decimal DepositAmount { get; }
        public decimal UpgradeAmount { get; }
        public int Status { get; }
    }

    public class CustomerDowngradeInfo
    {
        public CustomerDowngradeInfo(int vipLevel,
            decimal? downDeposit,
            decimal? downTurnover,
            int siteId,
            int currencyId,
            DateTime startTime,
            DateTime endTime,
            DateTime qualifiedTime,
            string calculateMonth)
        {
            VipLevel = vipLevel;
            DownDeposit = downDeposit ?? 0;
            DownTurnover = downTurnover ?? 0;
            SiteId = siteId;
            CurrencyId = currencyId;
            StartTime = startTime;
            EndTime = endTime;
            QualifiedTime = qualifiedTime;
            CalculateMonth = calculateMonth;
        }

        public int VipLevel { get; }
        public decimal DownDeposit { get; }
        public decimal DownTurnover { get; }
        public int SiteId { get; }
        public int CurrencyId { get; }
        public DateTime StartTime { get; }
        public DateTime EndTime { get; }

        public DateTime QualifiedTime { get; }

        public string CalculateMonth { get; }
    }

    public class VipDowngradeInfo
    {
        public VipDowngradeInfo(string calculateMonth,
            int siteId,
            int status)
        {
            SiteId = siteId;
            CalculateMonth = calculateMonth;
            Status = status;
        }

        public VipDowngradeInfo(int custId,
            string calculateMonth,
            int siteId,
            int currencyId,
            int currentVipLevel,
            int downgradeVipLevel,
            decimal depositAmount,
            decimal downgradeDepositAmount,
            decimal turnoverAmount,
            decimal downgradeTurnoverAmount,
            int status)
        {
            CustId = custId;
            CalculateMonth = calculateMonth;
            SiteId = siteId;
            CurrencyId = currencyId;
            CurrentVipLevel = currentVipLevel;
            DowngradeVipLevel = downgradeVipLevel;
            DepositAmount = depositAmount;
            DowngradeDepositAmount = downgradeDepositAmount;
            TurnoverAmount = turnoverAmount;
            DowngradeTurnoverAmount = downgradeTurnoverAmount;
            Status = status;
        }

        public int CustId { get; }
        public string CalculateMonth { get; }
        public int SiteId { get; }
        public int CurrencyId { get; }
        public int CurrentVipLevel { get; }
        public int DowngradeVipLevel { get; }
        public decimal DepositAmount { get; }
        public decimal DowngradeDepositAmount { get; }
        public decimal TurnoverAmount { get; }
        public decimal DowngradeTurnoverAmount { get; }
        public int Status { get; }
        public DateTime SettleTime { get; }
    }

    public class VipDowngradeReview
    {
        public int SiteId { get; set; }
        public int CustId { get; set; }
        public string UserName { get; set; }
        public string UserLang { get; set; }
        public int CurrentVipLevel { get; set; }
        public int DowngradeVipLevel { get; set; }
    }
}