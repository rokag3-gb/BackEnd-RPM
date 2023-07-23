using RPM.Domain.Dto;
using RPM.Domain.Models;
using RPM.Infra.Data;
using Dapper;

namespace RPM.Api.App.Queries;

public class InstanceJobQueries : IInstanceJobQueries
{
    private readonly RPMDbConnection _rpmDbConn;

    public InstanceJobQueries(RPMDbConnection salesDbConn)
    {
        _rpmDbConn = salesDbConn;
    }

    public IEnumerable<InstanceJob> GetInstanceJobs(long accountId, IEnumerable<long>? instanceIds)
    {
        using (var conn = _rpmDbConn.CreateConnection())
        {
            var queryTemplate = "select /**select**/ from Instance_Job AS J /**innerjoin**/ /**where**/";
            var selects =
                @"J.SNo, J.InstId, J.JobId, J.ActionCode, J.SavedAt";

            var builder = new SqlBuilder().Select(selects);
            builder = builder.InnerJoin("Instance AS I on J.InstId = I.InstId");
            builder = builder.Where("I.AccountId = @accId", new { accId = accountId });
            if(instanceIds != null && instanceIds.AsList().Count > 0){
                builder = builder.Where("I.InstId IN @instanceIds", new { instanceIds = instanceIds });
            }
            var template = builder.AddTemplate(queryTemplate);

            conn.Open();
            return conn.Query<InstanceJob>(template.RawSql, template.Parameters).AsList();
        }
    }

    /// <summary>
    /// InstanceId를 기준으로 On-Off 둘 다 설정되어 있는 레코드를 조회합니다
    /// </summary>
    public async Task<IEnumerable<InstanceJob>> GetInstanceJobsByOnOffPair()
    {
        using var conn = _rpmDbConn.CreateConnection();

        conn.Open();
        string sql = @"
select t1.InstId, t1.JobId, t1.ActionCode from instance_job as t1
where (exists(
    select null 
    from instance_job as t2
    where (t2.actioncode = 'ACT-TON') and (t1.instid = t2.instid)
    )) and (exists(
    select null 
    from instance_job as t3
    where (t3.actioncode = 'ACT-Off') and (t1.instid = t3.instid)
    ))";

        return await conn.QueryAsync<InstanceJob>(sql);
    }
}
