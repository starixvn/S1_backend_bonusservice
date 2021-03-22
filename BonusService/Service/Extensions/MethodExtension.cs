using System.Diagnostics;

namespace BonusService.Service.Extensions
{
    public static class MethodExtension
    {
        public static string GetCurrentMethod()
        {
            return new StackTrace().GetFrame(1)?.GetMethod()?.Name;
        }
    }
}