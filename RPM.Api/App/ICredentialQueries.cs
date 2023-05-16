using RPM.Domain.Dto;
using RPM.Domain.Models;
namespace RPM.Api.App;

public interface ICredentialQueries {
    IEnumerable<Credential> GetCredentials(long AccountId, string? Vendor = null, string? CredName = null, bool IsEnabled = true);
    Credential GetCredentialById(int id);
}