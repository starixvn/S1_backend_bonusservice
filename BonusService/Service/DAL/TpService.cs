using BonusService.Service.BLL;
using BonusService.Service.Library;
using BonusService.Service.Model;
using MemberSite.Service.Model;

namespace BonusService.Service.DAL;

public class TpService
{
    private readonly string _tpServiceUrl;
    private readonly CourierAdapter _courierAdapter;
    
    public TpService(IServiceProvider provider)
    {
        var configBll = provider.GetRequiredService<ConfigBLL>();
        _tpServiceUrl = configBll.GetConfigValueByEnvironment<ServiceHostConfig>("ServiceHost").TpService;
        _courierAdapter = provider.GetRequiredService<CourierAdapter>();
    }
    
    public async Task<ApiResult> AddFreeSpinToPlayer(AddFreeSpin request)
    {
        var url = _tpServiceUrl + "AddFreeSpinToPlayer";
        var result = await _courierAdapter.MyPostAsync<ApiResult, AddFreeSpin>(url, request);

        return result;
    }
    
}
