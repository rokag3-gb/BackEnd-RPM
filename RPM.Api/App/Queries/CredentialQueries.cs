using RPM.Domain.Dto;
using RPM.Domain.Models;
using RPM.Infra.Data;
using Dapper;

namespace RPM.Api.App.Queries;

public class CredentialQueries : ICredentialQueries
{
    private readonly RPMDbConnection _rpmDbConn;

    public CredentialQueries(RPMDbConnection salesDbConn)
    {
        _rpmDbConn = salesDbConn;
    }

    public IEnumerable<Credential> GetCredentials(
        long accountId,
        string? vendor = null,
        string? credName = null,
        bool? isEnabled = null
    )
    {
        using (var conn = _rpmDbConn.CreateConnection())
        {
            var queryTemplate = "select /**select**/ from Credential  /**where**/ /**orderby**/";
            var selects =
                @"CredId, AccountId, Vendor, CredName, IsEnabled, CredData, Note, SavedAt, SaverId";

            var builder = new SqlBuilder().Select(selects);
            builder = builder.Where("AccountId = @accId", new { accId = accountId });
            if (isEnabled.HasValue)
            {
                builder = builder.Where("IsEnabled = @isEnabled", new { isEnabled = isEnabled });
            }
            if (!string.IsNullOrEmpty(vendor))
            {
                builder = builder.Where("Vendor = @vendor", new { vendor = vendor });
            }
            if (!string.IsNullOrEmpty(credName))
            {
                builder = builder.Where("CredName = @credName", new { credName = credName });
            }

            builder = builder.OrderBy("SavedAt");

            var template = builder.AddTemplate(queryTemplate);

            conn.Open();
            return conn.Query<Credential>(template.RawSql, template.Parameters).AsList();
        }
    }

    public Credential? GetCredentialById(long accountId, long credId)
    {
        using (var conn = _rpmDbConn.CreateConnection())
        {
            var queryTemplate = "select /**select**/ from Credential  /**where**/ /**orderby**/";
            var selects =
                @"CredId, AccountId, Vendor, CredName, IsEnabled, CredData, Note, SavedAt, SaverId";

            var builder = new SqlBuilder().Select(selects);
            builder = builder.Where("AccountId = @accId", new { accId = accountId });
            builder = builder.Where("CredId = @credId", new { credId = credId });

            var template = builder.AddTemplate(queryTemplate);

            conn.Open();
            return conn.Query<Credential>(template.RawSql, template.Parameters).FirstOrDefault();
        }
    }

    public IEnumerable<Credential> GetCredentialsByIds(long accountId, IEnumerable<long> credentialIds)
    {
        using (var conn = _rpmDbConn.CreateConnection())
        {
            var queryTemplate = "select /**select**/ from Credential  /**where**/";
            var selects = @"CredId, AccountId, Vendor, CredName, IsEnabled, CredData, Note, SavedAt, SaverId";

            var builder = new SqlBuilder().Select(selects);
            builder = builder.Where("AccountId = @accId", new { accId = accountId });
            builder = builder.Where("CredId IN @credIds", new { credIds = credentialIds });

            var template = builder.AddTemplate(queryTemplate);

            conn.Open();
            return conn.Query<Credential>(template.RawSql, template.Parameters).AsList();
        }
    }
}
