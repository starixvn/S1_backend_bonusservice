using BonusService.Extensions;
using BonusService.Service.BLL;
using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.UnitTest.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace BonusService.UnitTest
{
    [TestFixture()]
    public class RebateBLLTests
    {
        private readonly IConfiguration _configuration;
        private readonly RebateBLL _rebateBLL;

        public RebateBLLTests()
        {
            var services = new ServiceCollection();

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
            {
                EnvironmentName = "Local",
            });
            builder.Configuration.AddRemoteJsonConfig(builder.Configuration.GetSection("ConfigService").Get<string[]>());
            var app = builder.Build();
            _configuration = app.Configuration;

            var env = new WebHostingEnvironment
            {
                EnvironmentName = Environments.Development,
                ContentRootPath = AppDomain.CurrentDomain.BaseDirectory,
            };
            services.AddSingleton(_configuration);
            services.AddTransient(p => Substitute.For<ILogger<MySqlClient>>());
            services.AddTransient(p => Substitute.For<ILogger<RebateBLL>>());
            services.AddSingleton<IWebHostEnvironment>(env);
            services.AddSingleton<IHostEnvironment>(env);

            var startup = new Startup(_configuration);
            startup.ConfigureServices(services);
            var provider = services.BuildServiceProvider();

            _rebateBLL = provider.GetRequiredService<RebateBLL>();

        }

        private List<VipRebateTpBetRecord> _vipRebateTpBetRecordList;
        
        [SetUp]
        public void Setup()
        {
            _vipRebateTpBetRecordList = new List<VipRebateTpBetRecord>()
            {
                new() { SettledDate = new DateTime(2023,04,01), CategoryId = 2, GameType = 68, GameID = "1", RedeemAmount = 100, ValidBet = 10000, Percentage = 1},
                new() { SettledDate = new DateTime(2023,04,01), CategoryId = 2, GameType = 68, GameID = "2", RedeemAmount = 100, ValidBet = 10000, Percentage = 1},
                new() { SettledDate = new DateTime(2023,04,01), CategoryId = 2, GameType = 68, GameID = "3", RedeemAmount = 100, ValidBet = 10000, Percentage = 1}
            };
        }

        [TestCase("SicBo")]
        public void PayoutVipRebateTest(string gameId)
        {
            var siteId = 1;
            var vipRebateInfo = new VipRebateInfo
            { 
                CurrencyId = 2,
                CustId = 250713,
                UserName= "hanstest001",
            };
            _vipRebateTpBetRecordList.AddRange(new List<VipRebateTpBetRecord>()
            {
                new() { SettledDate = new DateTime(2023,04,01), CategoryId = 2, GameType = 68, GameID = gameId, RedeemAmount = 100, ValidBet = 10000, Percentage = 1}
            });
            var rebatePayoutLimits = new List<RebatePayoutLimit>();
            var result = _rebateBLL.PayoutVipRebate(siteId, vipRebateInfo, _vipRebateTpBetRecordList, rebatePayoutLimits);
        }
    }
}
