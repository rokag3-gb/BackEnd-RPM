using RPM.Domain.Models;
using RPM.Domain.Dto;
namespace RPM.Infra.Data.Repositories;
public interface ICredentialRepository {
    
    Credential CreateSingleCredential(CredentialModifyDto credential);
    Credential UpdateSingleCredential(long credId, CredentialModifyDto credential);

    void DeleteSingleCredential(long accountId, long credId);
    int DeleteMultipleCredentials(long accountId, List<long> credIds);
}