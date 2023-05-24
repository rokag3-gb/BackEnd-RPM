using RPM.Domain.Commands;
using RPM.Domain.Models;
using RPM.Infra.Data;
using Dapper;

namespace RPM.Api.App.Repository;

public class InstanceRepository : IInstanceRepository
{
    private readonly RPMDbConnection _rpmDbConn;

    public InstanceRepository(RPMDbConnection salesDbConn)
    {
        _rpmDbConn = salesDbConn;
    }

    public Instance CreateSingleInstance(InstanceModifyCommand instance)
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

    public Instance UpdateSingleInstance(long instanceId, InstanceModifyCommand instance)
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

    public int DeleteSingleInstance(long accountId, long instanceId)
    {
        using (var conn = _rpmDbConn.CreateConnection())
        {
            var queryTemplate = @$"delete from Instance /**where**/";

            var builder = new SqlBuilder();

            builder = builder.Where("CredId = @credId", new { credId = instanceId });
            builder = builder.Where("AccountId = @accId", new { accId = accountId });

            var template = builder.AddTemplate(queryTemplate);

            conn.Open();
            return conn.Execute(template.RawSql, template.Parameters);
        }
    }

    Instance IInstanceRepository.CreateMultipleInstance(
        IEnumerable<InstanceModifyCommand> instances
    )
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
            var result = conn.QuerySingle<Instance>(queryTemplate, instances);
            return result;
        }
    }
}
