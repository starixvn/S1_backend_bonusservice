using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace BonusService.Service.DAL
{
    public class MessageService
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<MessageService> _logger;

        public MessageService(IConfiguration configuration, ILogger<MessageService> logger)
        {
            this.configuration = configuration;
            _logger = logger;

        }
        public int SendMessage(int siteId, string sender, string title, string content, int custId, string CustUserName, string lang)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Msg_Ins", new { Type = 99, SiteId = siteId, LangId = lang, RecipientCustId = custId, Recipient = CustUserName, SenderCustId = siteId, Sender = sender, Title = title, Content = content, MainId = 0 }, commandType: CommandType.StoredProcedure);
        }
    }
}
