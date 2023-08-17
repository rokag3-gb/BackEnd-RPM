using RPM.Domain.Dto;
using RPM.Domain.Models;
using System.Data;
using Dapper;

namespace RPM.Infra.Data.Repositories;

public class InstanceJobRepository : IInstanceJobRepository
{
    private readonly RPMDbConnection _rpmDbConn;

    public InstanceJobRepository(RPMDbConnection rpmDbConn)
    {
        _rpmDbConn = rpmDbConn;
    }

    public InstanceJob CreateSingleInstanceJob(InstanceJobModifyDto instance)
    {
        using (var conn = _rpmDbConn.CreateConnection())
        {
            var fields =
                "InstId, JobId, ActionCode";
            var queryTemplate =
                @$"insert into Instance_Job ({fields})
            output inserted.SNo, inserted.InstId, inserted.JobId, inserted.ActionCode, inserted.SavedAt
            values (@InstId, @JobId, @ActionCode)";

            conn.Open();
            var result = conn.QuerySingle<InstanceJob>(queryTemplate, instance);
            return result;
        }
    }
}
