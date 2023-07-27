using RPM.Domain.Models;

namespace RPM.Api.App.Queries
{
    public interface IInstancePriceQueries
    {
        Task<IEnumerable<InstancePrice>> Get(IEnumerable<long> instanceIds);
    }
}