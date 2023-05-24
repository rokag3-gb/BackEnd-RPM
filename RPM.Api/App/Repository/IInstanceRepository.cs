using RPM.Domain.Commands;
using RPM.Domain.Models;
namespace RPM.Api.App.Repository;

public interface IInstanceRepository {
    
    // Instance CreateSingleCredential(CredentialModifyCommand credential);
    // Instance UpdateSingleCredential(long credId, CredentialModifyCommand credential);

    int DeleteSingleInstance(long accountId, long instanceId);
}