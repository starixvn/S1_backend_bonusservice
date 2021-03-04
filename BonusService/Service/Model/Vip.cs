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
    }

    public class VipSetting
    {
        public int SiteId { get; set; }
        public int Status { get; set; }
        public int PeriodType { get; set; }
        public int PeriodInterval { get; set; }
        public int PreviousDateRangeType { get; set; }
        public int WithdrawalLimitRatio { get; set; }
        public DateTime SettleTime { get; set; }
        public DateTime LastUpdateTime { get; set; }
    }

    public class VipAmount
    {
        public decimal UpgradeAmount { get; set; }
        public decimal WithdrawalLimitAmount { get; set; }
    }

    public class CustomerUpgradeInfo
    {
        public CustomerUpgradeInfo(int vipLevel, decimal upDeposit, int siteId, int currencyId, DateTime startTime,
            DateTime endTime)
        {
            VipLevel = vipLevel;
            UpDeposit = upDeposit;
            SiteId = siteId;
            CurrencyId = currencyId;
            StartTime = startTime;
            EndTime = endTime;
        }

        public int VipLevel { get; }
        public decimal UpDeposit { get; }
        public int SiteId { get; }
        public int CurrencyId { get; }
        public DateTime StartTime { get; }
        public DateTime EndTime { get; }
    }

    public class CustomerVipInfo
    {
        public CustomerVipInfo(int oldLevel, int newLevel, int custId, string userName, int siteId,
            decimal upgradeAmount, decimal withdrawalLimitAmount, string financialMemo, string withdrawalLimitMemo)
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
    }
}
