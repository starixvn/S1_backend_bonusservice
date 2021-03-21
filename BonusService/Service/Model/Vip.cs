using System;

namespace BonusService.Service.Model
{
    public class VipSystem
    {
        public int SiteId { get; set;}
        public int CurrencyId { get; set;}
        public int Level { get; set;}
        public decimal UpDeposit { get; set;}
        public decimal UpBonus { get; set;}
        public decimal? DownDeposit { get; set; }
        public decimal? DownTurnover { get; set; }
    }

    public class CustVip
    {
        public int CustId { get; set;}
        public int VipLevel { get; set;}
        public string UserName { get; set;}
        public decimal DepositAmount { get; set; }
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
            decimal upgradeAmount, decimal withdrawalLimitAmount, string financialMemo, string withdrawalLimitMemo, string calculateMonth, DateTime settleTime)
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
            SettleTime = settleTime;
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
        public VipUpgradeInfo(string calculateMonth, int siteId, string userName, int status, DateTime upgradeTime)
        {
            SiteId = siteId;
            CalculateMonth = calculateMonth;
            UserName = userName;
            Status = status;
            UpgradeTime = upgradeTime;
        }

        public VipUpgradeInfo(int custId, string calculateMonth, string userName, int siteId, int currentVipLevel,
            int upgradeVipLevel, int currencyId, decimal depositAmount, decimal upgradeAmount, int status, DateTime settleTime)
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
            SettleTime = settleTime;
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
        public DateTime UpgradeTime { get; }
        public DateTime SettleTime { get; }
    }
}
