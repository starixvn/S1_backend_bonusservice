using BonusService.Service.BLL;
using BonusService.Service.Library;
using BonusService.Service.Model;
using MemberSite.Service.Model;

namespace BonusService.Service.DAL;

public class StoreService
{
    private readonly string _storeSiteUrl;
    private readonly CourierAdapter _courierAdapter;
    
    public StoreService(IServiceProvider provider)
    {
        var configBll = provider.GetRequiredService<ConfigBLL>();
        _storeSiteUrl = configBll.GetConfigValueByEnvironment<ServiceHostConfig>("ServiceHost").StoreService;
        _courierAdapter = provider.GetRequiredService<CourierAdapter>();
    }
    
    public async Task<ApiResult<TransIdModel>> ManualRewardPointAsync(CreateManualRewardPointInfoRequest request)
    {
        var url = _storeSiteUrl + "Member/ManualRewardPoint";
        var result = await _courierAdapter.MyPostAsync<TransIdModel, CreateManualRewardPointInfoRequest>(url, request);

        return result;
    }
    
}
