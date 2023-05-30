using RPM.Domain.Dto;
using RPM.Domain.Models;
using System.Data;
using Dapper;

namespace RPM.Infra.Data.Repositories;

public class InstanceRepository : IInstanceRepository
{
    private readonly RPMDbConnection _rpmDbConn;

    public InstanceRepository(RPMDbConnection salesDbConn)
    {
        _rpmDbConn = salesDbConn;
    }

    public Instance CreateSingleInstance(InstanceModifyDto instance)
    {
        using (var conn = _rpmDbConn.CreateConnection())
        {
            var fields =
                "AccountId, CredId, Vendor, ResourceId, Name, Region, Type, Tags, Info, Note, SaverId";
            var queryTemplate =
                @$"insert into Instance ({fields}) 
            output inserted.InstId, inserted.AccountId, inserted.CredId, inserted.Vendor, inserted.ResourceId, 
            inserted.Name, inserted.Region, inserted.Type, inserted.Tags, inserted.Info, inserted.Note, inserted.SavedAt, inserted.SaverId,
            values (@AccountId, @CredId, @Vendor, @ResourceId, @Name, @Region, @Type, @Tags, @Info, @Note, @SaverId)";

            conn.Open();
            var result = conn.QuerySingle<Instance>(queryTemplate, instance);
            return result;
        }
    }

    public IEnumerable<Instance> CreateMultipleInstances(IEnumerable<InstanceModifyDto> instances)
    {
        using (var conn = _rpmDbConn.CreateConnection())
        {
            var fields =
                "AccountId, CredId, Vendor, ResourceId, Name, Region, Type, Tags, Info, Note, SaverId";
            var queryTemplate =
                @$"insert into Instance ({fields}) 
            output inserted.InstId, inserted.AccountId, inserted.CredId, inserted.Vendor, inserted.ResourceId, 
            inserted.Name, inserted.Region, inserted.Type, inserted.Tags, inserted.Info, inserted.Note, inserted.SavedAt, inserted.SaverId,
            values (@AccountId, @CredId, @Vendor, @ResourceId, @Name, @Region, @Type, @Tags, @Info, @Note, @SaverId)";

            conn.Open();
            var result = conn.Query<Instance>(queryTemplate, instances).AsList();

            return result;
        }
    }

    public int CreateMultipleInstances(
        IEnumerable<InstanceModifyDto> instances,
        IDbConnection conn,
        IDbTransaction tx
    )
    {
        var fields =
            "AccountId, CredId, Vendor, ResourceId, Name, Region, Type, Tags, Info, Note, SaverId";
        var queryTemplate =
            @$"insert into Instance ({fields}) 
            values (@AccountId, @CredId, @Vendor, @ResourceId, @Name, @Region, @Type, @Tags, @Info, @Note, @SaverId)";

        var result = conn.Execute(queryTemplate, instances, tx);

        return result;
    }

    public Instance UpdateSingleInstance(long instanceId, InstanceModifyDto instance)
    {
        using (var conn = _rpmDbConn.CreateConnection())
        {
            var queryTemplate =
                @$"update Instance /**set**/
            output inserted.InstId, inserted.AccountId, inserted.CredId, inserted.Vendor, inserted.ResourceId, 
            inserted.Name, inserted.Region, inserted.Type, inserted.Tags, inserted.Info, inserted.Note, inserted.SavedAt, inserted.SaverId,
            /**where**/";

            var builder = new SqlBuilder();

            builder = builder.Set(
                @"AccountId = @AccountId, CredId = @CredId, Vendor = @Vendor, ResourceId = @ResourceId, 
                Name = @Name, Region = @Region, Type = @Type, Tags = @Tags, Info = @Info, Note = @Note, SaverId,
                SavedAt = getdate()",
                instance
            );
            builder = builder.Where("AccountId = @accId", new { accId = instance.AccountId });
            builder = builder.Where("InstanceId = @instanceId", new { instanceId = instanceId });

            var template = builder.AddTemplate(queryTemplate);

            conn.Open();
            var result = conn.QuerySingle<Instance>(template.RawSql, template.Parameters);
            return result;
        }
    }

    public IEnumerable<Instance> UpdateMultipleInstances(IEnumerable<Instance> instances)
    {
        using (var conn = _rpmDbConn.CreateConnection())
        {
            var queryTemplate =
                @$"update Instance 
            set AccountId = @AccountId, CredId = @CredId, Vendor = @Vendor, ResourceId = @ResourceId, Name = @Name,
            Region = @Region, Type = @Type, Tags = @Tags, Info = @Info, Note = @Note, SaverId, SavedAt = getdate()
            output inserted.InstId, inserted.AccountId, inserted.CredId, inserted.Vendor, inserted.ResourceId, 
            inserted.Name, inserted.Region, inserted.Type, inserted.Tags, inserted.Info, inserted.Note, inserted.SavedAt,
            inserted.SaverId where InstId = @InstId";

            conn.Open();
            var result = conn.Query<Instance>(queryTemplate, instances).AsList();
            return result;
        }
    }

    public int UpdateMultipleInstances(
        IEnumerable<Instance> instances,
        IDbConnection conn,
        IDbTransaction tx
    )
    {
        var queryTemplate =
            @$"update Instance 
            set AccountId = @AccountId, CredId = @CredId, Vendor = @Vendor, ResourceId = @ResourceId, Name = @Name,
            Region = @Region, Type = @Type, Tags = @Tags, Info = @Info, Note = @Note, SaverId = @SaverId, SavedAt = getdate()
            where InstId = @InstId";

        var result = conn.Execute(queryTemplate, instances, tx);
        return result;
    }

    public int DeleteSingleInstance(long accountId, long instanceId)
    {
        using (var conn = _rpmDbConn.CreateConnection())
        {
            var queryTemplate = @$"delete from Instance /**where**/";

            var builder = new SqlBuilder();

            builder = builder.Where("InstanceId = @instanceId", new { instanceId = instanceId });
            builder = builder.Where("AccountId = @accId", new { accId = accountId });

            var template = builder.AddTemplate(queryTemplate);

            conn.Open();
            return conn.Execute(template.RawSql, template.Parameters);
        }
    }

    public int DeleteMultipleInstances(IEnumerable<long> instanceIds)
    {
        using (var conn = _rpmDbConn.CreateConnection())
        {
            var queryTemplate = @$"delete from Instance where InstId = @instId";
            var queryParams = instanceIds.Select(x => new { instId = x });
            conn.Open();
            return conn.Execute(queryTemplate, queryParams);
        }
    }

    public int DeleteMultipleInstances(
        IEnumerable<long> instanceIds,
        IDbConnection conn,
        IDbTransaction tx
    )
    {
        var queryTemplate = @$"delete from Instance where InstId = @instId";
        var queryParams = instanceIds.Select(x => new { instId = x });
        return conn.Execute(queryTemplate, queryParams, tx);
    }

    public IDbConnection GetConnection() => _rpmDbConn.CreateConnection();
}
