using RPM.Domain.Dto;
using RPM.Domain.Models;
using System.Data;
namespace RPM.Infra.Data.Repositories;
public interface IInstanceJobRepository {
    
    InstanceJob CreateSingleInstanceJob(InstanceJobModifyDto instance);
    int DeleteSingleInstanceJob(long sNo);
    int DeleteByInstanceId(long instanceId);
}