using Dapper;
using Microsoft.VisualBasic;
using RPM.Domain.Models;
using RPM.Infra.Data;

namespace RPM.Api.App.Queries
{
    public class InstancePriceQueries : IInstancePriceQueries
    {
        private readonly RPMDbConnection _rpmDbConn;

        public InstancePriceQueries(RPMDbConnection rpmDbConn)
        {
            _rpmDbConn = rpmDbConn;
        }

        public async Task<IEnumerable<InstancePrice>> Get(IEnumerable<long> instanceIds)
        {
            using var conn = _rpmDbConn.CreateConnection();
            conn.Open();

            var queryTemplate = @"select /**select**/ from VW_Instance_Price as t0 /**where**/";
            var builder = new SqlBuilder();
            var template = builder.AddTemplate(queryTemplate);

            builder.Select(@"
[t0].[InstId], [t0].[unit] AS [Unit], [t0].[price_USD] AS [Price_USD], [t0].[price_KRW] AS [Price_KRW], 
[t0].[effectiveDate] AS [EffectiveDate], [t0].[region] AS [Region], [t0].[sku] AS [Sku], 
[t0].[Azure_OSDisk_OSType2], [t0].[Azure_ShortMeterName], [t0].[Azure_meterName], 
[t0].[Azure_subcategory], [t0].[AWS_PlatformDetails], [t0].[AWS_PlatformDetails2], [t0].[AWS_offerTermFullCode], 
[t0].[AWS_description], [t0].[AWS_beginRange], [t0].[AWS_endRange], [t0].[Google_SNos], 
[t0].[Google_MachineType], [t0].[Google_MachineType2], [t0].[Google_Preemptible], 
[t0].[Google_ConsumeReservationType], [t0].[Google_skuNames], [t0].[Google_resourceGroups], 
[t0].[Google_usageType], [t0].[Google_descriptions]");

            if (instanceIds != null && instanceIds.Count() > 0)
                builder = builder.Where("t0.InstId IN @InstanceIds", new { InstanceIds = instanceIds });

            return await conn.QueryAsync<InstancePrice>(template.RawSql, template.Parameters);
        }
    }
}
