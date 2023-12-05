using BonusService.Service.DAL;
using BonusService.Service.Model.enums;

namespace BonusService.Service.BLL
{
    public class QueueJobBLL
    {
        private readonly QueueJobService _queueJobService;

        public QueueJobBLL(IServiceProvider provider)
        {
            _queueJobService = provider.GetRequiredService<QueueJobService>();
        }

        public IEnumerable<T> GetQueueJob<T>(QueueJob type)
        {
            return _queueJobService.GetQueueJob<T>(type);
        }
        
        public int DeleteQueueJobByCustId(int custId , QueueJob type)
        {
            return _queueJobService.DeleteQueueJobByCustId(custId ,type);
        }
        
        public int DeleteQueueJobBySq(int sq)
        {
            return _queueJobService.DeleteQueueJobBySq(sq);
        }
    }
}