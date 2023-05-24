using RPM.Domain.Commands;
using RPM.Domain.Models;
namespace RPM.Api.App.Repository;

public interface IInstanceRepository {
    
    Instance CreateSingleInstance(InstanceModifyCommand instance);
    Instance CreateMultipleInstance(IEnumerable<InstanceModifyCommand> instance);
    Instance UpdateSingleInstance(long instanceId, InstanceModifyCommand instance);

    int DeleteSingleInstance(long accountId, long instanceId);
}