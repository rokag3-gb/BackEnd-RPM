using RPM.Domain.Dto;
using RPM.Domain.Models;
using System.Data;
namespace RPM.Infra.Data.Repositories;
public interface IInstanceRepository {
    
    Instance CreateSingleInstance(InstanceModifyDto instance);
    IEnumerable<Instance> CreateMultipleInstances(IEnumerable<InstanceModifyDto> instance);
    IEnumerable<Instance> CreateMultipleInstances(IEnumerable<InstanceModifyDto> instance, IDbConnection conn, IDbTransaction tx);
    Instance UpdateSingleInstance(long instanceId, InstanceModifyDto instance);
    IEnumerable<Instance> UpdateMultipleInstances(IEnumerable<Instance> instance);
    IEnumerable<Instance> UpdateMultipleInstances(IEnumerable<Instance> instance, IDbConnection conn, IDbTransaction tx);

    int DeleteSingleInstance(long accountId, long instanceId);
    int DeleteMultipleInstances(IEnumerable<long> instanceIds);
    int DeleteMultipleInstances(IEnumerable<long> instanceIds, IDbConnection conn, IDbTransaction tx);

    public IDbConnection GetConnection();
}