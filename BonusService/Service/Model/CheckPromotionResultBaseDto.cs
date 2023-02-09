using BonusService.Service.Model.enums;

namespace BonusService.Service.Model
{
    public class CheckPromotionResultBaseDto
    {
        public BonusErrorCode BonusErrorCode { get; set; }
        public bool UseWhitelist { get; set; }
        public string ErrorMsg { get; set; }
    }

    public class CheckPromotionBlockIp : CheckPromotionResultBaseDto
    {
        public bool IsBlockIp { get; set; }
    }

    public class CheckPromotionBlockDeviceId : CheckPromotionResultBaseDto
    {
        public bool IsBlockDeviceId { get; set; }
    }

    public class CheckPromotionBlockRealName : CheckPromotionResultBaseDto
    {
        public bool IsBlockRealName { get; set; }
    }

    public class CheckPromotionBlockTags : CheckPromotionResultBaseDto
    {
        public bool IsBlockTag { get; set; }
    }
}
