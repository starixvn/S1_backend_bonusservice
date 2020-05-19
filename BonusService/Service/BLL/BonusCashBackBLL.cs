using BonusService.Service.DAL;
using BonusService.Service.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BonusService.Service.BLL
{
    public class BonusCashBackBLL
    {
        private readonly BonusCashBackService GetBonusCashBackInfo;

        public BonusCashBackBLL(BonusCashBackService bonusCashBackService)
        {
            this.GetBonusCashBackInfo = bonusCashBackService;
        }

        public class BonusCashBackInfoList
        {
            public int CustId { get; set; }
            public string UserName { set; get; }
            public int CurrencyId { set; get; }
            public int SiteId { set; get; }
            public string TpUserName { set; get; }
            public decimal WinlostAmountCountCalculationTotal { set; get; }
            public List<BonusCashBackInfoListDetail> BonusCashBackInfoListDetails { get; set; }

        }

        public IEnumerable<BonusCashBack> BonusCashBackInfo()
        {

            var CashBackdataInfo = GetBonusCashBackInfo.GetBonusCashBackInfo();


            foreach (var CashBackdata in CashBackdataInfo)
            {
                Console.WriteLine($"{DateTimeOffset.Now} Id:{CashBackdata.Id} SiteId:{CashBackdata.SiteId} Period:{CashBackdata.Period} DayOfWeek:{CashBackdata.DayOfWeek} PayHour:{CashBackdata.PayHour} PayMinute:{CashBackdata.PayMinute} WithdrawCondition:{CashBackdata.WithdrawCondition} DefaultPercentage:{CashBackdata.DefaultPercentage} TimeZone:{CashBackdata.TimeZone}");

                DateTimeOffset trigerTime = new DateTimeOffset(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, CashBackdata.PayHour, CashBackdata.PayMinute, 0, new TimeSpan(0, Convert.ToInt32(CashBackdata.TimeZone * 60m), 0));
                //long StartDay = new DateTimeOffset(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day - 1, 0, 0, 0, new TimeSpan(0, Convert.ToInt32(CashBackdata.TimeZone * 60m), 0)).ToUnixTimeSeconds();
                //long EndDay = new DateTimeOffset(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day - 1, 23, 59, 59, new TimeSpan(0, Convert.ToInt32(CashBackdata.TimeZone * 60m), 0)).ToUnixTimeSeconds();
                long StartDay = new DateTimeOffset(2020, 01, 19, 0, 0, 0, new TimeSpan(0, Convert.ToInt32(CashBackdata.TimeZone * 60m), 0)).ToUnixTimeSeconds();
                long EndDay = new DateTimeOffset(2020, 01, 19, 23, 59, 59, new TimeSpan(0, Convert.ToInt32(CashBackdata.TimeZone * 60m), 0)).ToUnixTimeSeconds();

                string NowTime = DateTime.Now.ToString("yyyyMMddHHmm");
                string LocalTime = trigerTime.LocalDateTime.ToString("yyyyMMddHHmm");
                int LocalDayOfWeek = (int)trigerTime.DayOfWeek;
                List<BonusCashBackInfoList> bonusCashBackInfoLists = new List<BonusCashBackInfoList>();

                if (NowTime == LocalTime || true)
                {
                    if (CashBackdata.Period == 1)
                    {

                    }
                    else if (CashBackdata.Period == 2 && LocalDayOfWeek == CashBackdata.DayOfWeek || true)
                    {
                        var BonusCashBackGamesInfo = GetBonusCashBackInfo.GetBonusCashBackGamesInfo(CashbackId: CashBackdata.Id, SiteId: CashBackdata.SiteId);
                        foreach (var BonusCashBackGames in BonusCashBackGamesInfo)
                        {
                            if (BonusCashBackGames.Status == 1)
                            {
                                var TpBetRecordInfo = GetBonusCashBackInfo.GetTpBetRecordInfo(StartDay, EndDay, SiteId: CashBackdata.SiteId, GameType: BonusCashBackGames.GameId);

                                foreach (var TpBetRecord in TpBetRecordInfo)
                                {
                                    if (TpBetRecord.WinlostAmountCount < 0)
                                    {

                                        long value = (long)Math.Pow(10.0, 2);
                                        decimal Calculation = Convert.ToDecimal(Math.Floor(TpBetRecord.WinlostAmountCount * BonusCashBackGames.Percentage * (decimal)value) / (decimal)value) * -1;
                                        Console.WriteLine(
                                            $"{TpBetRecord.CustId}  " +
                                            $"{TpBetRecord.UserName}  " +
                                            $"{TpBetRecord.StakeCount}  " +
                                            $"{TpBetRecord.WinlostAmountCount}  " +
                                            $"{BonusCashBackGames.Percentage}  " +
                                            $"{Calculation} "
                                        );

                                        var get = bonusCashBackInfoLists.FirstOrDefault(x => x.CustId == TpBetRecord.CustId);
                                        if (get != null)
                                        {
                                            get.WinlostAmountCountCalculationTotal += Calculation;
                                            get.BonusCashBackInfoListDetails.Add(new BonusCashBackInfoListDetail() { GameType = BonusCashBackGames.GameId, StakeCount = TpBetRecord.StakeCount, WinlostAmountCount = TpBetRecord.WinlostAmountCount, WinlostAmountCountCalculation = Calculation });
                                        }
                                        else
                                        {
                                            var gameInfo = new List<BonusCashBackInfoListDetail>();
                                            gameInfo.Add(new BonusCashBackInfoListDetail() { GameType = BonusCashBackGames.GameId, StakeCount = TpBetRecord.StakeCount, WinlostAmountCount = TpBetRecord.WinlostAmountCount, WinlostAmountCountCalculation = Calculation });
                                            bonusCashBackInfoLists.Add(new BonusCashBackInfoList()
                                            {
                                                CustId = TpBetRecord.CustId,
                                                UserName = TpBetRecord.UserName,
                                                CurrencyId = TpBetRecord.CurrencyId,
                                                SiteId = CashBackdata.SiteId,
                                                TpUserName = TpBetRecord.TpUserName,
                                                WinlostAmountCountCalculationTotal = Calculation,
                                                BonusCashBackInfoListDetails = gameInfo
                                            });
                                        }
                                    }
                                }
                            }
                        }

                        //var BonusCashBackMinMaxInfo = GetBonusCashBackInfo.GetBonusCashBackMinMaxInfo(CashbackId: CashBackdata.Id, SiteId: CashBackdata.SiteId);

                        //foreach (var BonusCashBackMinMax in BonusCashBackMinMaxInfo)
                        //{
                        //    Console.WriteLine("--MinMax--");
                        //    Console.WriteLine(BonusCashBackMinMax.SiteId);
                        //    Console.WriteLine(BonusCashBackMinMax.CurrencyId);
                        //    Console.WriteLine(BonusCashBackMinMax.MinPay);
                        //    Console.WriteLine(BonusCashBackMinMax.MaxPay);
                        //    Console.WriteLine(BonusCashBackMinMax.MinBet);
                        //    Console.WriteLine(BonusCashBackMinMax.MaxBet);
                        //    Console.WriteLine("--MinMax--");
                        //}
                    }
                }

                if (bonusCashBackInfoLists != null)
                {
                    try
                    {
                        foreach (var bonusCashBackInfoList in bonusCashBackInfoLists)
                        {
                            Console.WriteLine(bonusCashBackInfoList.CustId);
                            Console.WriteLine(bonusCashBackInfoList.UserName);
                            Console.WriteLine(bonusCashBackInfoList.SiteId);
                            Console.WriteLine(bonusCashBackInfoList.WinlostAmountCountCalculationTotal);
                            GetBonusCashBackInfo.InsBonusCashBack(bonusCashBackInfoList.CustId, bonusCashBackInfoList.SiteId, bonusCashBackInfoList.UserName, bonusCashBackInfoList.WinlostAmountCountCalculationTotal, 7);
                        }
                    }
                    catch (Exception ex) {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            return GetBonusCashBackInfo.GetBonusCashBackInfo();
        }
    }
}
