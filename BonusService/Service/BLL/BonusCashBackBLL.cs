using BonusService.Service.DAL;
using BonusService.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BonusService.Service.BLL
{
    public class BonusCashBackBLL
    {
        private readonly BonusCashBackService bonusCashBackService;
        private readonly IMemoryCache cache;

        public BonusCashBackBLL(IMemoryCache cache, BonusCashBackService bonusCashBackService)
        {
            this.bonusCashBackService = bonusCashBackService;
            this.cache = cache;
        }

        public IEnumerable<BonusCashBack> GetBonusCashBackInfo()
        {
            string key = $"GetBonusCashBackInfo";

            var GetBonusCashBackInfo = cache.Get<IEnumerable<BonusCashBack>>(key);
            if (GetBonusCashBackInfo == null)
            {
                GetBonusCashBackInfo = bonusCashBackService.GetBonusCashBackInfo();

                if (GetBonusCashBackInfo != null)
                {
                    cache.Set(key, GetBonusCashBackInfo, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1)));
                }
            }

            return GetBonusCashBackInfo;
        }
        public IEnumerable<BonusCashBackGames> GetBonusCashBackGamesInfo(int CashbackId, int SiteId)
        {
            string key = $"GetBonusCashBackGamesInfo_{CashbackId}_{SiteId}";

            var GetBonusCashBackGamesInfo = cache.Get<IEnumerable<BonusCashBackGames>>(key);
            if (GetBonusCashBackGamesInfo == null)
            {
                GetBonusCashBackGamesInfo = bonusCashBackService.GetBonusCashBackGamesInfo(CashbackId, SiteId);

                if (GetBonusCashBackGamesInfo != null)
                {
                    cache.Set(key, GetBonusCashBackGamesInfo, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1)));
                }
            }

            return GetBonusCashBackGamesInfo;
        }
        public IEnumerable<TpBetRecord> GetTpBetRecordInfo(long StartDate, long EndDate, int SiteId, int GameType)
        {
            return bonusCashBackService.GetTpBetRecordInfo(StartDate, EndDate, SiteId, GameType);
        }
        public void BonusCashBackInfo()
        {

            var CashBackdataInfo = GetBonusCashBackInfo();

            foreach (var CashBackdata in CashBackdataInfo)
            {
                //Console.WriteLine($"{DateTimeOffset.Now} Id:{CashBackdata.Id} SiteId:{CashBackdata.SiteId} Period:{CashBackdata.Period} DayOfWeek:{CashBackdata.DayOfWeek} PayHour:{CashBackdata.PayHour} PayMinute:{CashBackdata.PayMinute} WithdrawCondition:{CashBackdata.WithdrawCondition} DefaultPercentage:{CashBackdata.DefaultPercentage} TimeZone:{CashBackdata.TimeZone}");

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
                        var BonusCashBackGamesInfo = bonusCashBackService.GetBonusCashBackGamesInfo(CashbackId: CashBackdata.Id, SiteId: CashBackdata.SiteId);
                        foreach (var BonusCashBackGames in BonusCashBackGamesInfo)
                        {
                            if (BonusCashBackGames.Status == 1)
                            {
                                var TpBetRecordInfo = bonusCashBackService.GetTpBetRecordInfo(StartDay, EndDay, SiteId: CashBackdata.SiteId, GameType: BonusCashBackGames.GameId);

                                foreach (var TpBetRecord in TpBetRecordInfo)
                                {
                                    if (TpBetRecord.WinlostAmountCount < 0)
                                    {
                                        decimal Percentage = (BonusCashBackGames.Percentage == null) ? CashBackdata.DefaultPercentage : BonusCashBackGames.Percentage;
                                        long value = (long)Math.Pow(10.0, 2);
                                        decimal Calculation = Convert.ToDecimal(Math.Floor(TpBetRecord.WinlostAmountCount * Percentage / 100 * (decimal)value) / (decimal)value) * -1;
                                        //Console.WriteLine(
                                        //    $"{TpBetRecord.CustId}  " +
                                        //    $"{TpBetRecord.UserName}  " +
                                        //    $"{TpBetRecord.StakeCount}  " +
                                        //    $"{TpBetRecord.WinlostAmountCount}  " +
                                        //    $"{BonusCashBackGames.Percentage}  " +
                                        //    $"{Calculation} "
                                        //);

                                        var get = bonusCashBackInfoLists.FirstOrDefault(x => x.CustId == TpBetRecord.CustId);
                                        if (get != null)
                                        {
                                            get.WinlostAmountCountCalculationTotal += Calculation;
                                            get.BonusCashBackInfoListDetails.Add(new BonusCashBackInfoListDetail() { GameType = BonusCashBackGames.GameId, StakeCount = TpBetRecord.StakeCount, WinlostAmountCount = TpBetRecord.WinlostAmountCount, WinlostAmountCountCalculation = Calculation });
                                        }
                                        else
                                        {
                                            var gameInfo = new List<BonusCashBackInfoListDetail>();
                                            gameInfo.Add(new BonusCashBackInfoListDetail() { GameType = BonusCashBackGames.GameId, StakeCount = TpBetRecord.StakeCount, WinlostAmountCount = TpBetRecord.WinlostAmountCount, WinlostAmountCountCalculation = Calculation, Percentage = Percentage });
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
                    foreach (var bonusCashBackInfoList in bonusCashBackInfoLists)
                    {
                        bonusCashBackService.InsBonusCashBack(bonusCashBackInfoList);
                    }
                }
            }
        }
    }
}
