using BonusService.Service.Model;

namespace BonusService.Service.DAL.Interface
{
    public interface ICommonService
    {
        IEnumerable<CurrencyTimeZoneDto> GetCurrencyTimeZones();
    }
}
