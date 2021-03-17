using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System.Data;
using System.Threading.Tasks;
using BonusService.Service.Library;
using BonusService.Service.Model;

namespace BonusService.Service.DAL
{
    public class MessageService
    {
        private readonly IConfiguration _configuration;
        private readonly MySqlClient _mainDbClient;

        public MessageService(IConfiguration configuration, ILogger<MySqlClient> logger)
        {
            this._configuration = configuration;
            _mainDbClient = new MySqlClient(configuration.GetConnectionString("ConnectionString"), logger);

        }
        public int SendMessage(int siteId, string sender, string title, string content, int custId, string CustUserName, string lang)
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Msg_Ins",
                new
                {
                    Type = 99, SiteId = siteId, LangId = lang, RecipientCustId = custId, Recipient = CustUserName,
                    SenderCustId = siteId, Sender = sender, Title = title, Content = content, MainId = 0
                }, commandType: CommandType.StoredProcedure);
        }
        
        public async Task<bool> SendMessageAsync(Message message)
        {
            var parameter = new DynamicParameters();
            parameter.Add("Type", 99);
            parameter.Add("SiteId", message.SiteId);
            parameter.Add("LangId", message.LangId);
            parameter.Add("RecipientCustId", message.CustId);
            parameter.Add("Recipient", message.CustUserName);
            parameter.Add("SenderCustId", message.SiteId);
            parameter.Add("Sender", message.Sender);
            parameter.Add("Title", message.Title);
            parameter.Add("Content", message.Content);
            parameter.Add("MainId", 0);
            return await _mainDbClient.ExecuteAsync("Msg_Ins", parameter);
        }
    }
}
