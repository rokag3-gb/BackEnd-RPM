using RPM.Domain.Dto;
using RPM.Domain.Models;

namespace RPM.Api.App.Queries;

public interface IInstanceJobQueries {
    IEnumerable<InstanceJob> GetInstanceJobs(long accountId, IEnumerable<long>? instanceIds = null);
    Task<IEnumerable<InstanceJob>> GetInstanceJobsAsync(long accountId, IEnumerable<long>? instanceIds = null);

    /// <summary>
    /// InstanceId를 기준으로 On-Off 둘 다 설정되어 있는 레코드를 조회합니다
    /// </summary>
    Task<IEnumerable<InstanceJob>> GetInstanceJobsByOnOffPair(long accountId);
}