using RPM.Domain.Models;

namespace RPM.Api.App.Queries
{
    public interface IInstanceSnapshotQueries
    {
        Task<InstanceSnapshot?> Get(long accountId, long instanceId, int year, int month);
        Task<IEnumerable<InstanceSnapshot>> List(long accountId, int year, int month);
    }
}