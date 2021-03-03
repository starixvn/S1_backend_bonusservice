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
        private readonly VipService _vipService;

        public VipBLL(VipService vipService)
        {
            this._vipService = vipService;
        }

        public IEnumerable<VipSystem> GetVipSystem(int siteId)
        {
            return _vipService.GetVipSystem(siteId);
        }
        
        public IEnumerable<VipSetting> GetVipSetting()
        {
            return _vipService.GetVipSetting();
        }
        
        public int SetVipSetting(VipSetting vipSetting)
        {
            return _vipService.SetVipSetting(vipSetting);
        }

        
        public IEnumerable<CustVip> GetCustUpgrade(CustomerUpgradeInfo customerUpgradeInfo)
        {
            return _vipService.GetCustUpgrade(customerUpgradeInfo);
        }

        public int SetCustVip(CustomerVipInfo customerVipInfo)
        {
            return _vipService.SetCustVip(customerVipInfo);
        }
    }
}
