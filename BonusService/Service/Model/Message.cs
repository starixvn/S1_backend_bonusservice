namespace BonusService.Service.Model
{
    public class Message
    {
        public Message(int siteId, string sender, string title, string content, int custId, string custUserName, string langId)
        {
            SiteId = siteId;
            Sender = sender;
            Title = title;
            Content = content;
            CustId = custId;
            CustUserName = custUserName;
            LangId = langId;
        }

        public int SiteId { get; }
        public string Sender { get; }
        public string Title { get; }
        public string Content { get; }
        public int CustId { get; }
        public string CustUserName { get; }
        public string LangId { get; }
    }
}