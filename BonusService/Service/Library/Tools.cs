namespace BonusService.Service.Library
{
    public class Common
    {
        private readonly string environmentConfig;

        public Common(IConfiguration configuration)
        {
            environmentConfig = configuration.GetSection("Environment").Get<string>();
        }
        /// <summary>
        /// 保留两位小数的金额
        /// </summary>
        /// <param name="d"></param>
        /// <param name="decimals"></param>
        /// <returns></returns>
        public decimal Floor(decimal d, int decimals)
        {
            var value = (long)Math.Pow(10.0, decimals);
            return Convert.ToDecimal(Math.Floor(d * value) / value);
        }

        public string GetEnv()
        {
            switch (environmentConfig)
            {
                case "Development":
                case "LOCAL":
                    return "D";

                case "SIT":
                    return "S";

                case "UAT":
                    return "U";

                case "PROD":
                    return "P";

                default:
                    return "D";
            }
        }
    }
}
