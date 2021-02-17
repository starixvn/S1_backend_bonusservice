using BonusService.Service.DAL;
using BonusService.Service.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BonusService.Service.BLL
{
    public class VipBLL
    {
        private readonly VipService vipService;

        public VipBLL(VipService vipService)
        {
            this.vipService = vipService;
        }

        public IEnumerable<VipSystem> GetVipSystem()
        {
            return vipService.GetVipSystem();
        }
        public IEnumerable<CustVip> GetCustUpgrade(int vipLevel, decimal upDeposit, int siteId, int currencyId)
        {
            return vipService.GetCustUpgrade(vipLevel, upDeposit, siteId, currencyId);
        }
        public int SetCustVip(int oldLevel, int newLevel, int custId, string userName, int siteId)
        {
            return vipService.SetCustVip(oldLevel, newLevel, custId, userName, siteId);
        }
    }
}
