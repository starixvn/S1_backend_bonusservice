using BonusService.Service.BLL;
using BonusService.Service.Extensions;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BonusService.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class APIController : ControllerBase
    {
        private readonly BonusAPIBLL _bonusAPIBLL;
        private readonly ILogger<APIController> _logger;

        public APIController(IServiceProvider provider, ILogger<APIController> logger)
        {
            _bonusAPIBLL = provider.GetRequiredService<BonusAPIBLL>();
            this._logger = logger;
        }

        public class SetAdjustBonusAsyncReq
        {
            public int CustId { get; set; }
            public string BonusCode { get; set; }
            public string Operator { get; set; }
            public string Memo { get; set; }

        }
        [HttpPost]
        public async Task<ApiResult<string>> SetAdjustBonusAsync(SetAdjustBonusAsyncReq setAdjustBonusReq)
        {
            var result = new ApiResult<string>();
            try
            {
                result = await _bonusAPIBLL.SetAdjustmentBonusAsync(setAdjustBonusReq.CustId, setAdjustBonusReq.BonusCode, setAdjustBonusReq.Operator, setAdjustBonusReq.Memo);
            }
            catch (Exception ex)
            {
                result.ErrorCode = 999;
                result.RawMessage = ex.ToString();
                _logger.LogError(ex, MethodExtension.GetCurrentMethod());
            }
            if (result.ErrorCode != 0)
            {
                _logger.LogError($"SetAdjustBonusAsync { Newtonsoft.Json.JsonConvert.SerializeObject(result)}");
            }
            return result;
        }
    }


}
