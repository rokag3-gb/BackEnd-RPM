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

    public IEnumerable<Instance> GetInstanceJobs(long accountId, IEnumerable<long> instanceIds)
    {
        using (var conn = _rpmDbConn.CreateConnection())
        {
            var queryTemplate = "select /**select**/ from Instance_Jobs J /**innerjoin**/ /**where**/";
            var selects =
                @"J.SNo, J.InstId, J.ActionCode, J.SavedAt";

            var builder = new SqlBuilder().Select(selects);
            builder.InnerJoin("Instance I", "I.InstId = J.InstId");
            builder = builder.Where("I.AccountId = @accId", new { accId = accountId });
            builder = builder.Where("I.InstId IN @instanceIds", new { instanceIds = instanceIds });
            var template = builder.AddTemplate(queryTemplate);

            conn.Open();
            return conn.Query<Instance>(template.RawSql, template.Parameters).AsList();
        }
    }
}
