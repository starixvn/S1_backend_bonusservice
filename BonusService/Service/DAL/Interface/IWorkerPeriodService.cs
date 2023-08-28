using BonusService.Service.Model;
using Dapper;

namespace BonusService.Service.DAL.Interface
{
    public interface IWorkerPeriodService
    {
        public IEnumerable<WorkerPeriodTask> GetTask(string workerName);

        public int SetTaskDone(int siteId, int currencyId, string workerName);
    }
}
