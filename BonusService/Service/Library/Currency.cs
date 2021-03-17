using System.Collections.Generic;

namespace BonusService.Service.Library
{
    
    public static class Currency
    {
        private static readonly Dictionary<int, string> _currency = new Dictionary<int, string>()
        {
            {2, "MYR"}, {4, "THB"}, {13, "RMB"}, {15, "IDR"}, {51, "VND"}
        };

        public static Dictionary<int, string> GetCurrency()
        {
            return _currency;
        }
    }
}