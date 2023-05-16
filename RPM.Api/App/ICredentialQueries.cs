using RPM.Domain.Dto;
using RPM.Domain.Models;
namespace RPM.Api.App;

public interface ICredentialQueries {
    IEnumerable<Credential> GetCredentials(long AccountId, string? Vendor = null);
    Credential GetCredentialById(int id);
}