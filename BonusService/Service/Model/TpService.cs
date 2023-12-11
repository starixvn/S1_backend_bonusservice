namespace BonusService.Service.Model
{
    public class AddFreeSpin
    {
        public int TpId { set; get; }
        public int CustId { set; get; }
        public int SiteId { set; get; }
        public Dictionary<string, string> inputs { set; get; }
    }
}