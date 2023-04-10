namespace BonusService.Service.Model
{
    public class Message
    {
        public Message(string sender, int custId, string langId, string title, string content)
        {
            CustId = custId;
            LangId = langId;
            Title = title;
            Content = content;
            Sender = sender;
        }
        public int CustId { get; }
        public string LangId { get; }
        public string Title { get; }
        public string Content { get; }
        public string Sender { get; }
    }
}