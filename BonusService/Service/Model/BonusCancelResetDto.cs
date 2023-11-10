namespace BonusService.Service.Model
{
    public class BonusCancelResetDto
    {
        public int BonusId { get; set; }
        public int CustId { get; set; }
        public int SiteId { get; set; }
        public string UserName { get; set; }
        public int PromoCategoryId { get; set; }
    }
}
