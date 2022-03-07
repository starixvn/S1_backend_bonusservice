using BonusService.Service.Model.enums;

namespace BonusService.Service.Model
{
    public class AllowIp
    {
        public string IP { get; set; }
        public AllowIpType Type { get; set; }
    }
}