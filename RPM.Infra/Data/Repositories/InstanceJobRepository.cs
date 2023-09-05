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
            var fields = "InstId, JobId, ActionCode";
            var queryTemplate =
                @$"insert into Instance_Job ({fields})
            output inserted.SNo, inserted.InstId, inserted.JobId, inserted.ActionCode, inserted.SavedAt
            values (@InstId, @JobId, @ActionCode)";

            conn.Open();
            var result = conn.QuerySingle<InstanceJob>(queryTemplate, instance);
            return result;
        }
    }

    public int DeleteSingleInstanceJob(long sNo)
    {
        using (var conn = _rpmDbConn.CreateConnection())
        {
            var queryTemplate = @$"delete from Instance_Job /**where**/";

            var builder = new SqlBuilder();

            builder = builder.Where("SNo = @sNo", new { sNo = sNo });

            var template = builder.AddTemplate(queryTemplate);

            conn.Open();
            return conn.Execute(template.RawSql, template.Parameters);
        }
    }

    public int DeleteByInstanceIds(
        IEnumerable<long> instanceIds,
        IDbConnection conn,
        IDbTransaction tx
    )
    {
        var queryTemplate = @$"delete from Instance_Job where InstId = @instId";
        var queryParams = instanceIds.Select(x => new { instId = x });
        return conn.Execute(queryTemplate, queryParams, tx);
    }
}
