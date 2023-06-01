using AutoMapper;
using RPM.Domain.Dto;
using RPM.Api.App.Commands;

namespace RPM.Api.App.Mappers;
public class CredentialMapperProfile : Profile
{
    public CredentialMapperProfile()
    {
        CreateMap<CredentialModifyDto, CredentialModifyCommand>();
        CreateMap<CredentialModifyCommand, CredentialModifyDto>();
    }
}