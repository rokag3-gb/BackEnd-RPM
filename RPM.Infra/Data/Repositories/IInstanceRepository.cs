using RPM.Domain.Dto;
using RPM.Domain.Models;
using System.Data;
namespace RPM.Infra.Data.Repositories;
public interface IInstanceRepository {
    
    Instance CreateSingleInstance(InstanceModifyDto instance);
    IEnumerable<Instance> CreateMultipleInstances(IEnumerable<InstanceModifyDto> instance);
    int CreateMultipleInstances(IEnumerable<InstanceModifyDto> instance, IDbConnection conn, IDbTransaction tx);
    Instance UpdateSingleInstance(long instanceId, InstanceModifyDto instance);
    Instance UpdateSingleInstanceNote(long instanceId, InstanceNoteModifyDto instance);
    IEnumerable<Instance> UpdateMultipleInstances(IEnumerable<Instance> instance);
    int UpdateMultipleInstances(IEnumerable<Instance> instance, IDbConnection conn, IDbTransaction tx);

    int DeleteSingleInstance(long accountId, long instanceId);
    int DeleteMultipleInstances(IEnumerable<long> instanceIds);
    int DeleteMultipleInstances(IEnumerable<long> instanceIds, IDbConnection conn, IDbTransaction tx);

    public IDbConnection GetConnection();
}