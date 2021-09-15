using MemberSite.Service.Model;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace MemberSite.Service.Library
{
    public class CourierAdapter
    {
        private static readonly Lazy<HttpClient> lazy = new Lazy<HttpClient>(() =>
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        });

        private HttpClient client => lazy.Value;
        private readonly ILogger<CourierAdapter> _logger;
        
        public CourierAdapter(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<CourierAdapter>>();
        }

        public async Task<ApiResult<T>> MyPostAsync<T, U>(string url, U oTpInfo)
        {
            ApiResult<T> oApiResult;
            var fooJson = JsonConvert.SerializeObject(oTpInfo);
            LogContext.PushProperty("TpInfo", fooJson);
            LogContext.PushProperty("Tp.Request.URL", url);
            var fooContent = new StringContent(fooJson, Encoding.UTF8, "application/json");

            try
            {
                using var response = await client.PostAsync(url, fooContent);
                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Tp.Request.Response:{result}");

                if (response.IsSuccessStatusCode)
                {
                    oApiResult = JsonConvert.DeserializeObject<ApiResult<T>>(result);
                }
                else
                {
                    oApiResult = new ApiResult<T>
                    {
                        ErrorCode = 9000,
                        RawMessage = $"{result} {url}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                oApiResult = new ApiResult<T>
                {
                    ErrorCode = 9000,
                    RawMessage = $"{ex.Message} {url}"
                };
            }
            return oApiResult;
        }
    }
}
