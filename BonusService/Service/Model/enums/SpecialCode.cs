namespace BonusService.Service.Model.enums
{
    public enum SpecialCode
    {
        Success = 0,
        SpecialCodeError = 99,
        SpecialCodeErrorForCheck = 1000,
        SpecialCodeVipLevelsOrBlockTagInvalid = 1001, // VipLevel 不符合 或是有 BlockTag
        PromoCodeHasBeenUsed = 3000
    }

}
