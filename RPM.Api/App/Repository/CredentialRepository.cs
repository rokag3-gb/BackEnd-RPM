using RPM.Domain.Commands;
using RPM.Domain.Models;
using RPM.Infra.Data;
using Dapper;

namespace RPM.Api.App.Repository;

public class CredentialRepository : ICredentialRepository
{
    private readonly RPMDbConnection _rpmDbConn;

    public CredentialRepository(RPMDbConnection salesDbConn)
    {
        _rpmDbConn = salesDbConn;
    }

    public Credential CreateSingleCredential(CredentialModifyCommand credential)
    {
        using (var conn = _rpmDbConn.CreateConnection())
        {
            var fields = "AccountId, Vendor, CredName, IsEnabled, CredData, Note, SaverId";
            var queryTemplate = @$"insert into Credential ({fields}) 
            output inserted.CredId, inserted.AccountId, inserted.Vendor, inserted.CredName,
            inserted.IsEnabled, inserted.CredData, inserted.Note, inserted.SavedAt, inserted.SaverId
            values (@AccountId, @Vendor, @CredName, @IsEnabled, @CredData, @Note, @SaverId)";
            
            conn.Open();
            var result = conn.QuerySingle<Credential>(queryTemplate, credential);
            return result;
        }
    }

    public Credential UpdateSingleCredential(long credId, CredentialModifyCommand credential)
    {
        using (var conn = _rpmDbConn.CreateConnection())
        {
            var queryTemplate = @$"update Credential /**set**/
            output inserted.CredId, inserted.AccountId, inserted.Vendor, inserted.CredName,
            inserted.IsEnabled, inserted.CredData, inserted.Note, inserted.SavedAt, inserted.SaverId
            /**where**/";

            var builder = new SqlBuilder();
            
            builder = builder.Set(
                @"AccountId = @AccountId, Vendor = @Vendor, CredName = @CredName, 
                IsEnabled = @IsEnabled, CredData = @CredData, Note = @Note,
                SaverId = @SaverId, SavedAt = getdate()",
                credential);
            builder = builder.Where("AccountId = @accId", new { accId = credential.AccountId });
            builder = builder.Where("CredId = @credId", new { credId = credId });

            var template = builder.AddTemplate(queryTemplate);

            conn.Open();
            var result = conn.QuerySingle<Credential>(template.RawSql, template.Parameters);
            return result;
        }
    }

    public void DeleteSingleCredential(long accountId, long credId)
    {
        using (var conn = _rpmDbConn.CreateConnection())
        {
            var queryTemplate = @$"delete from Credential /**where**/";

            var builder = new SqlBuilder();
            
            builder = builder.Where("CredId = @credId", new { credId = credId });
            builder = builder.Where("AccountId = @accId", new { accId = accountId });

            var template = builder.AddTemplate(queryTemplate);

            conn.Open();
            conn.Execute(template.RawSql, template.Parameters);
        }
    }
}
