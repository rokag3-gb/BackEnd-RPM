using RPM.Domain.Dto;
using RPM.Domain.Models;
namespace RPM.Api.App.Queries;

public interface IInstanceJobQueries {
    IEnumerable<Instance> GetInstanceJobs(long accountId, IEnumerable<long> instanceIds);
}