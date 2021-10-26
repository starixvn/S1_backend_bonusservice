using BonusService.Service.BLL;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace BonusService.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class APIController : ControllerBase
    {
        private readonly BonusAPIBLL _bonusAPIBLL;

        public APIController(IServiceProvider provider)
        {
            _bonusAPIBLL = provider.GetRequiredService<BonusAPIBLL>();
        }

        public class SetAdjustBonusAsyncReq
        {
            public int CustId { get; set; }
            public string BonusCode { get; set; }
        }
        [HttpPost]
        public async Task<ApiResult> SetAdjustBonusAsync(SetAdjustBonusAsyncReq setAdjustBonusReq)
        {
            var result = new ApiResult();
            try
            {
                result = await _bonusAPIBLL.SetAdjustmentBonusAsync(setAdjustBonusReq.CustId, setAdjustBonusReq.BonusCode);
            }
            catch (Exception ex)
            {
                result.ErrorCode = 999;
                result.RawMessage = ex.ToString();
            }

            return result;
        }
    }


}
