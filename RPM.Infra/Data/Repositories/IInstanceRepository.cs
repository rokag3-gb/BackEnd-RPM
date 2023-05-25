using RPM.Domain.Dto;
using RPM.Domain.Models;
namespace RPM.Infra.Data.Repositories;
public interface IInstanceRepository {
    
    Instance CreateSingleInstance(InstanceModifyDto instance);
    IEnumerable<Instance> CreateMultipleInstance(IEnumerable<InstanceModifyDto> instance);
    Instance UpdateSingleInstance(long instanceId, InstanceModifyDto instance);

    int DeleteSingleInstance(long accountId, long instanceId);
}