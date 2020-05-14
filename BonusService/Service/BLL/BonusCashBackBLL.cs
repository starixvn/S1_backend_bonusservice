using System;
using System.Collections.Generic;
using BonusService.Service.DAL;
using BonusService.Service.Model;

namespace BonusService.Service.BLL
{
    public class BonusCashBackBLL
    {
        private readonly BonusCashBackService GetBonusCashBackInfo;


        public BonusCashBackBLL(BonusCashBackService bonusCashBackService)
        {
            this.GetBonusCashBackInfo = bonusCashBackService;
        }

        public IEnumerable<BonusCashBack> BonusCashBackInfo()
        {

            var CashBackdataInfo = GetBonusCashBackInfo.GetBonusCashBackInfo();

            foreach (var CashBackdata in CashBackdataInfo)
            {
                Console.WriteLine($"{DateTimeOffset.Now} Id:{CashBackdata.Id} SiteId:{CashBackdata.SiteId} Period:{CashBackdata.Period} DayOfWeek:{CashBackdata.DayOfWeek} PayHour:{CashBackdata.PayHour} PayMinute:{CashBackdata.PayMinute} WithdrawCondition:{CashBackdata.WithdrawCondition} DefaultPercentage:{CashBackdata.DefaultPercentage} TimeZone:{CashBackdata.TimeZone}");

                DateTimeOffset trigerTime = new DateTimeOffset(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, CashBackdata.PayHour, CashBackdata.PayMinute, 0, new TimeSpan(0, Convert.ToInt32(CashBackdata.TimeZone * 60m), 0));
                Console.WriteLine(DateTime.Now.ToString("yyyyMMddHHmm"));
                Console.WriteLine(trigerTime.LocalDateTime.ToString("yyyyMMddHHmm"));

                Console.WriteLine((int)DateTime.Today.DayOfWeek);
                Console.WriteLine((int)trigerTime.DayOfWeek);
                Console.WriteLine(CashBackdata.DayOfWeek);


                if (DateTime.Now.ToString("yyyyMMddHHmm") == trigerTime.LocalDateTime.ToString("yyyyMMddHHmm") || true)
                {
                    if (CashBackdata.Period == 1)
                    {

                    }
                    else if (CashBackdata.Period == 2 && (int)DateTime.Today.DayOfWeek == CashBackdata.DayOfWeek || true)
                    {
                        var BonusCashBackGamesInfo = GetBonusCashBackInfo.GetBonusCashBackGamesInfo(CashbackId: CashBackdata.Id, SiteId: CashBackdata.SiteId);

                        foreach (var BonusCashBackGames in BonusCashBackGamesInfo)
                        {
                            if (BonusCashBackGames.Status == 1)
                            {
                                Console.WriteLine("--Game--");
                                Console.WriteLine(BonusCashBackGames.TpId);
                                Console.WriteLine(BonusCashBackGames.Percentage);
                                Console.WriteLine(BonusCashBackGames.Status);
                                Console.WriteLine("--Game--");
                            }
                        }

                        var BonusCashBackMinMaxInfo = GetBonusCashBackInfo.GetBonusCashBackMinMaxInfo(CashbackId: CashBackdata.Id, SiteId: CashBackdata.SiteId);

                        foreach (var BonusCashBackMinMax in BonusCashBackMinMaxInfo)
                        {
                            Console.WriteLine("--MinMax--");
                            Console.WriteLine(BonusCashBackMinMax.SiteId);
                            Console.WriteLine(BonusCashBackMinMax.CurrencyId);
                            Console.WriteLine(BonusCashBackMinMax.MinPay);
                            Console.WriteLine(BonusCashBackMinMax.MaxPay);
                            Console.WriteLine(BonusCashBackMinMax.MinBet);
                            Console.WriteLine(BonusCashBackMinMax.MaxBet);
                            Console.WriteLine("--MinMax--");
                        }
                    }
                }
            }


            return GetBonusCashBackInfo.GetBonusCashBackInfo();
        }

    }
}
