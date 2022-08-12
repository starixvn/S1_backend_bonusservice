namespace BonusService.Service.Library
{
    public class Currency
    {
        private readonly Dictionary<int, string> _currency = new Dictionary<int, string>()
        {
            {2, "MYR"}, {4, "THB"}, {13, "CNY"}, {15, "IDR"}, {51, "VND"}
        };
        public Currency()
        { }

        public Dictionary<int, string> GetCurrency()
        {
            return _currency;
        }
    }
}