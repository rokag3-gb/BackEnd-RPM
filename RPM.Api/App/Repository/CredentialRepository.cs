using RPM.Domain.Dto;
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

    public long CreateSingleCredential(CredentialModifyCommand credential)
    {
        using (var conn = _rpmDbConn.CreateConnection())
        {
            var fields = "AccountId, Vendor, CredName, IsEnabled, CredData, Note";
            var queryTemplate = $"insert into Credential ({fields}) output inserted.CredId values (@AccountId, @Vendor, @CredName, @IsEnabled, @CredData, @Note)";
            
            conn.Open();
            var credId = conn.ExecuteScalar<long>(queryTemplate, credential);
            return credId;
        }
    }
}
