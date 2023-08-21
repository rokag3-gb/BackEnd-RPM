using RPM.Domain.Dto;
using RPM.Domain.Models;
using RPM.Infra.Data;
using Dapper;
using System.Data;

namespace RPM.Api.App.Queries;

public class InstanceJobQueries : IInstanceJobQueries
{
    private readonly RPMDbConnection _rpmDbConn;

    public InstanceJobQueries(RPMDbConnection salesDbConn)
    {
        _rpmDbConn = salesDbConn;
    }
   
    public IEnumerable<InstanceJob> GetInstanceJobs(long accountId, IEnumerable<long>? instanceIds= null, IEnumerable<long>? jobIds = null)
    {
        using (var conn = _rpmDbConn.CreateConnection())
        {
            var template = BuildGetInstanceJob(accountId, instanceIds, jobIds);
            conn.Open();
            return conn.Query<InstanceJob>(template.RawSql, template.Parameters).AsList();
        }
    }

    public async Task<IEnumerable<InstanceJob>> GetInstanceJobsAsync(long accountId, IEnumerable<long>? instanceIds= null)
    {
        using var conn = _rpmDbConn.CreateConnection();
        var template = BuildGetInstanceJob(accountId, instanceIds);
        conn.Open();
        return await conn.QueryAsync<InstanceJob>(template.RawSql, template.Parameters);
    }

    public async Task<IEnumerable<InstanceJob>> GetInstanceJobsByOnOffPair(long accountId)
    {
        using var conn = _rpmDbConn.CreateConnection();
        conn.Open();

        var queryTemplate = "select t1.InstId, t1.JobId, t1.ActionCode from instance_job as t1 /**innerjoin**/ /**where**/";

        var builder = new SqlBuilder();
        var template = builder.AddTemplate(queryTemplate);
        builder = builder.InnerJoin("Instance i on t1.InstId = i.InstId");
        builder = builder.Where(@"
            (exists(select null from instance_job as t2
            where (t2.actioncode = 'ACT-TON') and (t1.instid = t2.instid))) 
            and (exists(select null from instance_job as t3
            where (t3.actioncode = 'ACT-OFF') and (t1.instid = t3.instid))) 
            and i.AccountId = @AccountId", new { AccountId = accountId });

        return await conn.QueryAsync<InstanceJob>(template.RawSql, template.Parameters);
    }

    private SqlBuilder.Template BuildGetInstanceJob(long accountId,
                                                   IEnumerable<long>? instanceIds,
                                                   IEnumerable<long>? jobIds = null)
    {
        var queryTemplate = "select /**select**/ from Instance_Job AS J /**innerjoin**/ /**where**/";
        var selects =
            @"J.SNo, J.InstId, J.JobId, J.ActionCode, J.SavedAt";

        var builder = new SqlBuilder().Select(selects);
        builder = builder.InnerJoin("Instance AS I on J.InstId = I.InstId");
        builder = builder.Where("I.AccountId = @accId", new { accId = accountId });
        if (instanceIds != null && instanceIds.AsList().Count > 0)
        {
            builder = builder.Where("I.InstId IN @instanceIds", new { instanceIds = instanceIds });
        }
        if (jobIds != null && jobIds.Count() > 0)
            builder = builder.Where("J.JobId IN @jobIds", new { jobIds = jobIds });
        var template = builder.AddTemplate(queryTemplate);
        return template;
    }

}
