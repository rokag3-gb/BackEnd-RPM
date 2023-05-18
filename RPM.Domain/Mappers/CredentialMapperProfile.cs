using AutoMapper;
using RPM.Domain.Dto;
using RPM.Domain.Commands;
namespace RPM.Domain.Mappers;
public class CredentialMapperProfile : Profile
{
    public CredentialMapperProfile()
    {
        CreateMap<CredentialModifyDto, CredentialModifyCommand>();

    }
}