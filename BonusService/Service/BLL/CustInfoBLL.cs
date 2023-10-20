using BonusService.Service.DAL;
using BonusService.Service.Model;

namespace BonusService.Service.BLL;

public class CustInfoBLL
{
    private readonly CustInfoService _custInfoService;

    public CustInfoBLL(CustInfoService custInfoService)
    {
        _custInfoService = custInfoService;
    }

    public Customer GetCustInfo(int custId)
        => _custInfoService.GetCustInfo(custId);
}
