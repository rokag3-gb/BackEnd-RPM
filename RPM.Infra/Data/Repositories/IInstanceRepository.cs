using RPM.Domain.Dto;
using RPM.Domain.Models;
namespace RPM.Infra.Data.Repositories;
public interface IInstanceRepository {
    
    Instance CreateSingleInstance(InstanceModifyDto instance);
    IEnumerable<Instance> CreateMultipleInstances(IEnumerable<InstanceModifyDto> instance);
    Instance UpdateSingleInstance(long instanceId, InstanceModifyDto instance);
    IEnumerable<Instance> UpdateMultipleInstances(IEnumerable<Instance> instance);

    int DeleteSingleInstance(long accountId, long instanceId);
    int DeleteMultipleInstances(IEnumerable<long> instanceIds);
}