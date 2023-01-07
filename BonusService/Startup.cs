using BonusService.Extensions;
using BonusService.Service.BLL;
using BonusService.Service.DAL;
using BonusService.Service.DAL.Interface;
using BonusService.Service.Extensions;
using BonusService.Service.Library;
using BonusService.Service.Model;
using StackExchange.Redis;

namespace BonusService
{

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IConfiguration _configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(_configuration.GetValue<string>("RedisConnection")));
            services.AddSingleton<ICacheHelper, RedisCacheHelper>();
            services.AddDatabaseConnection(_configuration);
            services.AddSingleton<Common>();
            services.AddSingleton<Currency>();
            services.AddSingleton<CourierAdapter>();
            services.AddSingleton<CacheHelper>();

            services.AddSingleton<MessageService>();
            services.AddSingleton<CustInfoService>();
            services.AddSingleton<WalletService>();
            services.AddSingleton<WithdrawalLimitService>();
            services.AddSingleton<RebateService>();
            services.AddSingleton<VipRebateService>();
            services.AddSingleton<ExpiredBonusService>();
            services.AddSingleton<ReferralService>();
            services.AddSingleton<RewardService>();
            services.AddSingleton<VipService>();
            services.AddSingleton<VipCashBackService>();
            services.AddSingleton<PromotionAutoExtendService>();
            services.AddSingleton<DepositBonusService>();
            services.AddSingleton<ConfigService>();
            services.AddSingleton<IWalletAdapter, WalletAdapter>();
            services.AddSingleton<IWithdrawalLimitAdapter, WithdrawalLimitAdapter>();
            services.AddSingleton<IReferralService, ReferralService>();

            services.AddSingleton<RebateBLL>();
            services.AddSingleton<ReferralBLL>();
            services.AddSingleton<RewardBLL>();
            services.AddSingleton<ReferralReward>();
            services.AddSingleton<SelLangBLL>();
            services.AddSingleton<ExpiredBonusBLL>();
            services.AddSingleton<VipBLL>();
            services.AddSingleton<VipCashBackBLL>();
            services.AddSingleton<PromotionAutoExtendBLL>();
            services.AddSingleton<DepositBonusBLL>();

            services.AddSingleton<BonusAPIBLL>();
            services.AddSingleton<BonusAPIService>();
            services.AddSingleton<BonusWalletService>();
            services.AddSingleton<PromotionService>();
            services.AddSingleton<PromotionBll>();
            services.AddSingleton<AngPaoEventBLL>();
            services.AddSingleton<AngPaoEventService>();

            services.AddControllers();
            services.AddOptionListByDB<AllowIp>("Service_MemberAllowIP_Get");
        }
       
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseRequestLogMiddleware();
            app.UseResponseLogMiddleware();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/health", async context =>
                {
                    await context.Response.WriteAsync($"{env.EnvironmentName}-{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}");
                });
                endpoints.MapControllers();
            });
        }
    }
}