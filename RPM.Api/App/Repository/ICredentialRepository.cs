using RPM.Domain.Dto;
using RPM.Domain.Models;
namespace RPM.Api.App.Repository;

public interface ICredentialRepository {
    
    long CreateSingleCredential(CredentialModifyCommand credential);
}