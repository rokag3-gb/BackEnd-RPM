using RPM.Domain.Commands;
using RPM.Domain.Models;
namespace RPM.Api.App.Repository;

public interface ICredentialRepository {
    
    Credential CreateSingleCredential(CredentialModifyCommand credential);
    Credential UpdateSingleCredential(long credId, CredentialModifyCommand credential);

    void DeleteSingleCredential(long accountId, long credId);
}