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
}
