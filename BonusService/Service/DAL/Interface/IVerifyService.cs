using BonusService.Service.Model;

namespace BonusService.Service.DAL.Interface
{
    public interface IVerifyService
    {
        public bool IsRealNameVaild(Customer custInfo, IEnumerable<int> custIds, out List<CompareRealNameDto> realNameCheckResult);
    }
}
