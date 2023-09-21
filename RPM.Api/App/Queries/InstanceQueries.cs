using RPM.Domain.Dto;
using RPM.Domain.Models;
using RPM.Infra.Data;
using Dapper;
using Microsoft.Identity.Client;
using System.Numerics;

namespace RPM.Api.App.Queries;

public class InstanceQueries : IInstanceQueries
{
    private readonly RPMDbConnection _rpmDbConn;

    public InstanceQueries(RPMDbConnection salesDbConn)
    {
        _rpmDbConn = salesDbConn;
    }

    public IEnumerable<Instance> GetInstances(
        long accountId, long? credId, string? vendor = null, string? resourceId = null,
        string? name = null, string? region = null, string? type = null, bool? isEnable = null
    )
    {
        using (var conn = _rpmDbConn.CreateConnection())
        {
            var template = BuildGetInstances(accountId, credId, vendor, resourceId, name, region, type, isEnable);
            conn.Open();
            return conn.Query<Instance>(template.RawSql, template.Parameters).AsList();
        }
    }

    public async Task<IEnumerable<Instance>> GetInstancesAsync(long accountId,
                                                               long? credId = null,
                                                               string? vendor = null,
                                                               string? resourceId = null,
                                                               string? name = null,
                                                               string? region = null,
                                                               string? type = null)
    {
        using var conn = _rpmDbConn.CreateConnection();
        var template = BuildGetInstances(accountId, credId, vendor, resourceId, name, region, type);
        conn.Open();
        return await conn.QueryAsync<Instance>(template.RawSql, template.Parameters);
    }

    public Instance? GetInstanceById(long accountId, long instanceId)
    {
        using (var conn = _rpmDbConn.CreateConnection())
        {
            var queryTemplate = "select /**select**/ from Instance /**where**/ /**orderby**/";
            var selects =
                @"InstId, AccountId, CredId, Vendor, ResourceId, IsEnable, Name, Region, Type, Tags, Info, Note, SavedAt, SaverId";

            var builder = new SqlBuilder().Select(selects);
            builder = builder.Where("AccountId = @accId", new { accId = accountId });
            builder = builder.Where("InstId = @instanceId", new { instanceId = instanceId });

            var template = builder.AddTemplate(queryTemplate);

            conn.Open();
            return conn.Query<Instance>(template.RawSql, template.Parameters).FirstOrDefault();
        }
    }

    public IEnumerable<Instance> GetInstancesByIds(long accountId, IEnumerable<long> instanceIds)
    {
        using (var conn = _rpmDbConn.CreateConnection())
        {
            var queryTemplate = "select /**select**/ from Instance /**where**/";
            var selects =
                @"InstId, AccountId, CredId, Vendor, ResourceId, IsEnable, Name, Region, Type, Tags, Info, Note, SavedAt, SaverId";

            var builder = new SqlBuilder().Select(selects);
            builder = builder.Where("AccountId = @accId", new { accId = accountId });
            builder = builder.Where("InstId IN @instanceIds", new { instanceIds = instanceIds });
            var template = builder.AddTemplate(queryTemplate);

            conn.Open();
            return conn.Query<Instance>(template.RawSql, template.Parameters).AsList();
        }
    }

    private SqlBuilder.Template BuildGetInstances(long accountId,
                                                  long? credId,
                                                  string? vendor = null,
                                                  string? resourceId = null,
                                                  string? name = null,
                                                  string? region = null,
                                                  string? type = null,
                                                  bool? isEnable = null)
    {
        var queryTemplate = "select /**select**/ from Instance /**where**/ /**orderby**/";
        var selects =
            @"InstId, AccountId, CredId, Vendor, ResourceId, IsEnable, Name, Region, Type, Tags, Info, Note, SavedAt, SaverId";

        var builder = new SqlBuilder().Select(selects);
        builder = builder.Where("AccountId = @accId", new { accId = accountId });
        if (credId.HasValue)
        {
            builder = builder.Where("CredId = @credId", new { credId = credId });
        }
        if (!string.IsNullOrEmpty(vendor))
        {
            builder = builder.Where("Vendor = @vendor", new { vendor = vendor });
        }
        if (!string.IsNullOrEmpty(resourceId))
        {
            builder = builder.Where("ResourceId = @resourceId", new { resourceId = resourceId });
        }
        if (!string.IsNullOrEmpty(name))
        {
            builder = builder.Where("Name = @name", new { name = name });
        }
        if (!string.IsNullOrEmpty(region))
        {
            builder = builder.Where("Region = @region", new { region = region });
        }
        if (!string.IsNullOrEmpty(type))
        {
            builder = builder.Where("Type = @type", new { type = type });
        }
        if(isEnable.HasValue){
            builder = builder.Where("IsEnable = @isEnable", new { isEnable = isEnable.Value });
        }

        builder = builder.OrderBy("SavedAt");

        return builder.AddTemplate(queryTemplate);
    }
}
