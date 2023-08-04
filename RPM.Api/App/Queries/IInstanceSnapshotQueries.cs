using RPM.Domain.Models;

namespace RPM.Api.App.Queries
{
    public interface IInstanceSnapshotQueries
    {
        Task<IEnumerable<InstanceSnapshot>> List(long accountId, int year);
    }
}