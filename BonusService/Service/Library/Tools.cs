using Microsoft.Extensions.Hosting;
using System;

namespace BonusService.Service.Library
{
    public class Common
    {
        private readonly IHostEnvironment environment;
        public Common(IHostEnvironment environment)
        {
            this.environment = environment;
        }
        /// <summary>
        /// 保留两位小数的金额
        /// </summary>
        /// <param name="d"></param>
        /// <param name="decimals"></param>
        /// <returns></returns>
        public decimal Floor(decimal d, int decimals)
        {
            long value = (long)Math.Pow(10.0, decimals);
            return Convert.ToDecimal(Math.Floor(d * (decimal)value) / (decimal)value);
        }
        public string GetEnv()
        {
            if (environment.IsDevelopment())
            {
                return "D";
            }
            else if (environment.IsStaging())
            {
                return "S";
            }
            else if (environment.IsProduction())
            {
                return "P";
            }
            else if (environment.IsEnvironment("QA"))
            {
                return "Q";
            }
            return "X";
        }
    }
}
