using BonusService.Service.Model.enums;
using System;
using System.Collections.Generic;

namespace BonusService.Service.Model
{
    public class Customer
    {
        public int CustId { get; set; }
        public int SiteId { get; set; }
        public string UserName { get; set; }
        public string Userpwd { get; set; }
        public int CurrencyId { get; set; }
        public string UserLang { get; set; }
        public int ActStatus { get; set; }
        public int AgentId { get; set; }
        public string AllAgentId { get; set; }
        public int UserLevel { get; set; }
        public bool IsTest { get; set; }
        public DateTime LastLoginTime { get; set; }
        public DateTime RegTime { get; set; }
        public int Status { get; set; }
        public bool IsResetPwd { get; set; }
        public string Aff { get; set; }
        public string SiteName { get; set; }
    }
    public class WalletInfo
    {
        public decimal Balance { set; get; }
        public string LastLoginTime { set; get; }
        public string LastTransaction { set; get; }
        public bool IsHideBalance { set; get; }
        public IEnumerable<TpWallet> tpWallets { set; get; }
        public IEnumerable<BonusWallet> bonusWallets { set; get; }
    }
    public class TpWallet
    {
        public int TpId { set; get; }
        public string displayKey { get; set; }
        public string TpName { set; get; }
        public string Description { set; get; }
        public decimal Balance { set; get; }
        public decimal Bonus { set; get; }
        public bool Transfer { set; get; }
        public WalletStatus walletStatus { set; get; }
    }
    public class BonusWallet
    {
        public int BonusId { get; set; }
        public decimal Bonus { get; set; }
        public decimal Rollover { get; set; }
        public decimal Turnover { get; set; }
        public int PromoCategoryId { get; set; }
        public int TpId { get; set; }
        public int Status { get; set; }
        public DateTime? ExpiryTime { get; set; }
    }
    public class CustInfoProfile
    {
        public string UserName { get; set; }
        public int CurrencyId { get; set; }
        public int UserLevel { get; set; }
        public string Agent { get; set; }
        public string UserLang { get; set; }
        public int ActStatus { get; set; }
        public bool IsTest { get; set; }
        public DateTime LastLoginTime { get; set; }
        public DateTime RegTime { get; set; }
        public string Email { get; set; }
        public string AccountName { get; set; }
        public string CountryCode { get; set; }
        public string AreaCode { get; set; }
        public string PhoneNo { get; set; }
        public string Birth_d { get; set; }
        public string Birth_m { get; set; }
        public string Birth_y { get; set; }
        public string RealName { get; set; }
        public bool PhoneVerified { get; set; }
        public decimal PhoneReward { get; set; }
        public bool EmailVerified { get; set; }
        public decimal EmailReward { get; set; }
        public int EnablePromotion { get; set; }
    }
    
}
