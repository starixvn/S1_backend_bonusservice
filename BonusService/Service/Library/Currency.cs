namespace BonusService.Service.Library
{
    public class Currency
    {
        private readonly Dictionary<int, string> _currency = new Dictionary<int, string>()
        {
            {2,"MYR" },{3,"US" },{4,"THB" },{13,"CNY" },{15,"IDR" },{32,"JPY" },{45,"KRW" },{51,"VND" },{61,"INR" },{72,"USD" },{74,"BD" },{82,"BRL" },
        };
        public Currency()
        { }

        public Dictionary<int, string> GetCurrency()
        {
            return _currency;
        }
    }
}