namespace BonusService.Service.Model;

public class BonusFulfill
{
    public int BonusId { get; set; }
    public int PromotionCategoryId { get; set; }
    public int TpId { get; set; }
    public int CustId { get; set; }
    public int SiteId { get; set; }
    public string UserName { get; set; }
    public string Name { get; set; }
    public int WalletType { get; set; }
}