using RPM.Domain.Dto;
using RPM.Domain.Models;

namespace RPM.Api.App.Queries;

public interface IInstanceQueries {
    IEnumerable<Instance> GetInstances(
        long accountId, long? credId, string? vendor = null, string? resourceId = null,
        string? name = null, string? region = null, string? type = null, bool? isEnable = null);
    Instance? GetInstanceById(long accountId, long instanceId);
    IEnumerable<Instance> GetInstancesByIds(long accountId, IEnumerable<long> instanceIds);
    Task<IEnumerable<Instance>> GetInstancesAsync(long accountId, long? credId = null, string? vendor = null, string? resourceId = null, string? name = null, string? region = null, string? type = null);
}