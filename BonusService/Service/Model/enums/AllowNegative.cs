using System.ComponentModel;

namespace BonusService.Service.Model.enums
{
    [Description("是否允許餘額變負值")]
    public enum AllowNegative
    {
        Allow = 1,
        NotAllow = 0
    }
}