using System.Collections.Generic;
using System.IO;
using System.Linq;
using BonusService.Service.BLL;
using BonusService.Service.DAL;
using BonusService.Service.Library;
using BonusService.Service.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace BonusService.UnitTest;

public class ReferralTests
{
    private List<ReferralInfo> referralInfos;
    [SetUp]
    public void Setup()
    {
        referralInfos = new List<ReferralInfo>()
        {
            new() { CustId = 1, UserName = "test1", AgentId = 250032, AgentName = "hulkchou", CurrencyId = 2, SiteId = 1, BonusAmount = 50, ReferralFreeCredit = 10},
            new() { CustId = 2, UserName = "test2", AgentId = 250032, AgentName = "hulkchou", CurrencyId = 2, SiteId = 1, BonusAmount = 50, ReferralFreeCredit = 10},
            new() { CustId = 3, UserName = "test3", AgentId = 250764, AgentName = "hulkchou4", CurrencyId = 4, SiteId = 1, BonusAmount = 80, ReferralFreeCredit = 20},
            new() { CustId = 4, UserName = "test4", AgentId = 250764, AgentName = "hulkchou4", CurrencyId = 4, SiteId = 1, BonusAmount = 80, ReferralFreeCredit = 20},
            new() { CustId = 5, UserName = "test5", AgentId = 250764, AgentName = "hulkchou4", CurrencyId = 4, SiteId = 1, BonusAmount = 80, ReferralFreeCredit = 20},
            new() { CustId = 21, UserName = "test21", AgentId = 250797, AgentName = "hulkchou15", CurrencyId = 15, SiteId = 1, BonusAmount = 500000, ReferralFreeCredit = 10000 },
            new() { CustId = 22, UserName = "test22", AgentId = 250797, AgentName = "hulkchou15", CurrencyId = 15, SiteId = 1, BonusAmount = 500000, ReferralFreeCredit = 10000 },
            new() { CustId = 31, UserName = "test31", AgentId = 250765, AgentName = "hulkchou61", CurrencyId = 61, SiteId = 1, BonusAmount = 888, ReferralFreeCredit = 66},
            new() { CustId = 41, UserName = "test41", AgentId = 250385, AgentName = "hulkchou", CurrencyId = 51, SiteId = 2, BonusAmount = 5000, ReferralFreeCredit = 7000 },
            new() { CustId = 42, UserName = "test42", AgentId = 250385, AgentName = "hulkchou", CurrencyId = 51, SiteId = 2, BonusAmount = 5000, ReferralFreeCredit = 7000 },
        };
    }

    [Test]
    public void ListToLookUpTest()
    {
        var origin = new Dictionary<int, List<ReferralInfo>>();
        foreach(var referralInfo in referralInfos)
        {
            if (!origin.ContainsKey(referralInfo.AgentId))
            {
                origin.Add(referralInfo.AgentId, new List<ReferralInfo>());
            }

            origin[referralInfo.AgentId].Add(referralInfo);
        }

        var actual = referralInfos.ToLookup(a => a.AgentId);
        var isContainKey = actual.Aggregate(true, (current, settlement) => current & origin.ContainsKey(settlement.Key));
        
        var isSameElement = actual.Aggregate(true, (current, settlement) => current & origin[settlement.Key].Count == settlement.Count());
        Assert.IsTrue(isContainKey);
        Assert.IsTrue(isSameElement);
    }

    [Test]
    public void PayoutReferralTest()
    {
        var services = new ServiceCollection();
        var configBuilder = new ConfigurationBuilder();
        configBuilder.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false, false)
            .AddJsonFile("appsettings.Local.json", false, false);
        var configuration = configBuilder.Build();
        var logger = Substitute.For<ILogger<RebateService>>();
        var mySqlClient = Substitute.For<IEnumerable<MySqlClient>>();
        var walletService = Substitute.For<WalletService>();
        var withdrawalLimitService = Substitute.For<WithdrawalLimitService>();
        var referralSvc = new ReferralService(configuration, logger, walletService, withdrawalLimitService, mySqlClient);
        var referrals = referralInfos.ToLookup(a => a.AgentId);

        var isSuccess = true;
        foreach(var referral in referrals)
        {
            isSuccess &= referralSvc.PayoutReferral(referral, out var payAmount, out var referralInfos) > 0;
        }
        Assert.IsTrue(isSuccess);
    }
}