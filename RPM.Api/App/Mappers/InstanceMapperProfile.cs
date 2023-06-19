using AutoMapper;
using RPM.Domain.Dto;
using RPM.Domain.Models;
using RPM.Api.App.Commands;

namespace RPM.Api.App.Mappers;
public class InstanceMapperProfile : Profile
{
    public InstanceMapperProfile()
    {
        CreateMap<Instance, InstanceModifyDto>();
        CreateMap<InstanceModifyCommand, InstanceModifyDto>();
    }
}