using RPM.Domain.Dto;
using RPM.Domain.Models;
using RPM.Infra.Data;
using Dapper;

namespace RPM.Api.App;

public class CredentialQueries : ICredentialQueries
{
    private readonly RPMDbConnection _rpmDbConn;

    public CredentialQueries(RPMDbConnection salesDbConn)
    {
        _rpmDbConn = salesDbConn;
    }


    public IEnumerable<Credential> GetCredentials(
        long AccountId, string? Vendor = null,
        string? CredName = null, bool IsEnabled = true)
    {
   
        using (var conn = _rpmDbConn.CreateConnection())
        {
            var queryTemplate =
                "select /**select**/ from Credential  /**where**/ /**orderby**/";
            var selects = @"CredId, AccountId, Vendor, CredName, IsEnabled, CredData, Note, SavedAt, SaverId";

            var builder = new SqlBuilder().Select(selects);
            builder = builder.Where("AccountId = @accId", new { accId = AccountId });
            builder = builder.OrderBy("SavedAt");

            var template = builder.AddTemplate(queryTemplate);

            conn.Open();
            return conn.Query<Credential>(template.RawSql, template.Parameters).AsList();
        }
    }

    Credential ICredentialQueries.GetCredentialById(int id)
    {
        throw new NotImplementedException();
    }
}
