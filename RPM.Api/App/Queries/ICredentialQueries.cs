using RPM.Domain.Dto;
using RPM.Domain.Models;
namespace RPM.Api.App.Queries;

public interface ICredentialQueries {
    IEnumerable<Credential> GetCredentials(
        long accountId, string? vendor = null,
        string? credName = null, bool? isEnabled = null);
    Credential? GetCredentialById(long accountId, long credId);
}