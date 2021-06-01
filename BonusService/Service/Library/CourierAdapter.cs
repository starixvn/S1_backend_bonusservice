using MemberSite.Service.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace MemberSite.Service.Library
{
    public class CourierAdapter
    {
        private static Lazy<HttpClient> lazy = new Lazy<HttpClient>(() =>
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        });

        private HttpClient client { get { return lazy.Value; } }
        public async Task<ApiResult<T>> MyPostAsync<T, U>(string url, U oTpInfo)
        {
            ApiResult<T> oApiResult = null;

            var fooJSON = JsonConvert.SerializeObject(oTpInfo);
            var fooContent = new StringContent(fooJSON, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(url, fooContent);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                oApiResult = JsonConvert.DeserializeObject<ApiResult<T>>(responseBody);
            }
            else
            {
                oApiResult = new ApiResult<T>();
                oApiResult.ErrorCode = 9000;
                string responseBody = await response?.Content?.ReadAsStringAsync();
                oApiResult.RawMessage = $"{responseBody} {url}";
            }
            return oApiResult;
        }
    }
}
