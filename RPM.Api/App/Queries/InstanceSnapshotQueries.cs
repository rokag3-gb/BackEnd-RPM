using Dapper;
using RPM.Domain.Models;
using RPM.Infra.Data;

namespace RPM.Api.App.Queries
{
    public class InstanceSnapshotQueries : IInstanceSnapshotQueries
    {
        private readonly RPMDbConnection _rpmDbConn;

        public InstanceSnapshotQueries(RPMDbConnection rpmDbConn)
        {
            _rpmDbConn = rpmDbConn;
        }

        public async Task<IEnumerable<InstanceSnapshot>> List(long accountId, int year, int month)
        {
            using var conn = _rpmDbConn.CreateConnection();
            conn.Open();

            var queryTemplate = @"select /**select**/ from Instance_Snapshot as t0 /**where**/";
            var builder = new SqlBuilder();
            var template = builder.AddTemplate(queryTemplate);

            builder.Select(@"
[t0].[SNo], [t0].[SnapshotMonth], [t0].[InstId], [t0].[AccountId], [t0].[CredId], 
[t0].[Vendor], [t0].[ResourceId], [t0].[IsEnable], [t0].[Name], [t0].[Region], 
[t0].[Type], [t0].[Tags], [t0].[Info], [t0].[Note], [t0].[SavedAt], [t0].[SaverId]");

            builder.Where("AccountId = @accountId", new { accountId = accountId });
            builder.Where("IsEnable = @isEnable", new { isEnable = true });

            var to = new DateTime(year, month, 01);
            var from = to.AddMonths(-11);
            List<string> snapshotMonths = new List<string>();

            for (var dt = from; dt <= to; dt = dt.AddMonths(1))
            {
                snapshotMonths.Add(dt.ToString("yyyyMM"));
            }

            builder.Where("t0.SnapshotMonth in @months", new { months = snapshotMonths });

            return await conn.QueryAsync<InstanceSnapshot>(template.RawSql, template.Parameters);
        }

        public async Task<InstanceSnapshot?> Get(long accountId, long instanceId, int year, int month)
        {
            using var conn = _rpmDbConn.CreateConnection();
            conn.Open();

            var queryTemplate = @"select /**select**/ from Instance_Snapshot as t0 /**where**/";
            var builder = new SqlBuilder();
            var template = builder.AddTemplate(queryTemplate);

            builder.Select(@"
[t0].[SNo], [t0].[SnapshotMonth], [t0].[InstId], [t0].[AccountId], [t0].[CredId], 
[t0].[Vendor], [t0].[ResourceId], [t0].[IsEnable], [t0].[Name], [t0].[Region], 
[t0].[Type], [t0].[Tags], [t0].[Info], [t0].[Note], [t0].[SavedAt], [t0].[SaverId]");

            builder.Where("AccountId = @accountId", new { accountId = accountId });
            builder.Where("InstId = @InstId", new { InstId = instanceId });
            builder.Where("IsEnable = @isEnable", new { isEnable = true });
            builder.Where("t0.SnapshotMonth = @date", new { date = $"{year.ToString("0000")}{month.ToString("00")}" });

            var res = await conn.QueryAsync<InstanceSnapshot>(template.RawSql, template.Parameters);
            return res.FirstOrDefault();
        }
    }
}
