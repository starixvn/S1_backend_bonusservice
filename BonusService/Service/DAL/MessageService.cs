using BonusService.Service.Library;
using BonusService.Service.Model;
using BonusService.Service.Model.enums;

using Dapper;

namespace BonusService.Service.DAL
{
    public class MessageService
    {
        private readonly MySqlClient _mainDbClient;

        public MessageService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }
        public int SendMessage(Message message) =>
            _mainDbClient.Execute("Msg_Ins_V4", new DynamicParameters(message));

        public async Task<bool> SendMessageAsync(Message message) =>
            await _mainDbClient.ExecuteAsync("Msg_Ins_V4", new DynamicParameters(message));
    }
}
