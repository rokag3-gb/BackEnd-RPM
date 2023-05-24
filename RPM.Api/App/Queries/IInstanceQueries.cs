using RPM.Domain.Dto;
using RPM.Domain.Models;
namespace RPM.Api.App.Queries;

public interface IInstanceQueries {
    IEnumerable<Instance> GetInstances(
        long accountId, string? vendor = null, string? resourceId = null,
        string? name = null, string? region = null, string? type = null);
    Instance? GetInstanceById(long accountId, long instanceId);
}