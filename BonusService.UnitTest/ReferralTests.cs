using BonusService.Service.BLL;
using BonusService.Service.DAL.Interface;
using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
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
        var referralService = Substitute.For<IReferralService>();
        var fakeWallet = new FakeWalletService();
        var fakeWithdrawalLimitService = new FakeWithdrawalLimitService();
        var mySqlClient = Substitute.For<IEnumerable<MySqlClient>>();
        var logger = Substitute.For<ILogger<ReferralReward>>();
        var referralReward = new ReferralReward(referralService, fakeWallet, fakeWithdrawalLimitService, mySqlClient, logger);
        var referrals = new List<ReferralInfo>();
        var result = referralReward.Payout(referrals, out var payAmount, out var paidReferralList);

        result.Should().Be(0);
        payAmount.Should().Be(0);
        paidReferralList.Count.Should().Be(0);
    }

    [Test]
    public void Two_Referral_And_Remaing_For_Three()
    {
        var referralService = Substitute.For<IReferralService>();
        referralService.GetRemainingCount(Arg.Any<int>(), Arg.Is(1), Arg.Is(2)).Returns(x => 3);
        var fakeWallet = new FakeWalletService();
        var fakeWithdrawalLimitService = new FakeWithdrawalLimitService();
        var mysqlLogger = Substitute.For<ILogger<MySqlClient>>();
        var sqlClient = new MySqlClient("server=13.250.152.76;port=3306;user=worker_caching_sha2;database=Siebog;password=Aa12345678!!!;SslMode=None;AllowPublicKeyRetrieval=true;", mysqlLogger)
        {
            DataBase = DataBase.Main
        };
        var mySqlClient = new List<MySqlClient> { sqlClient };
        var logger = Substitute.For<ILogger<ReferralReward>>();
        var referralReward = new ReferralReward(referralService, fakeWallet, fakeWithdrawalLimitService, mySqlClient, logger);
        var referrals = new List<ReferralInfo>()
        {
            new() { CustId = 1, UserName = "test1", AgentId = 250032, AgentName = "hulkchou", CurrencyId = 2, SiteId = 1, BonusAmount = 50, ReferralFreeCredit = 10},
            new() { CustId = 2, UserName = "test2", AgentId = 250032, AgentName = "hulkchou", CurrencyId = 2, SiteId = 1, BonusAmount = 50, ReferralFreeCredit = 10},
        };
        
        var result = referralReward.Payout(referrals, out var payAmount, out var paidReferralList);

        result.Should().Be(1);
        payAmount.Should().Be(100);
        fakeWallet._customers.Count.Should().Be(3);
    }

    public class FakeWalletService : IWalletAdapter
    {
        public List<TestCustomer> _customers = new();

        public int SetBalance(int custId, int siteId, string userName, decimal amount, int type, string memo, string refTransId, out decimal afterAmount, out byte is1StDeposit, MySqlTransaction trans)
        {
            _customers.Add(new TestCustomer()
            {
                CustId = custId,
                Amount = amount,
            });
            afterAmount = amount;
            is1StDeposit = 0;
            return 1;
        }
    }
    
    public class FakeWithdrawalLimitService : IWithdrawalLimitAdapter
    {
        public readonly List<TestCustomer> _Customers = new();
        public decimal WithdrawalLimit { get; set; }
        public int SetWithdrawalLimit(int custId, string userName, int siteId, decimal withdrawalLimit, string memo, string refTransId, out decimal afterWithdrawalLimit, MySqlTransaction trans)
        {
            afterWithdrawalLimit = withdrawalLimit;
            WithdrawalLimit = withdrawalLimit;
            return 1;
        }
    }
    
    public class TestCustomer
    {
        public int CustId { get; set; }
        public decimal Amount { get; set; }
        public decimal WithdrawalLimit { get; set; }
    }
}