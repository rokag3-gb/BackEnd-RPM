using MediatR;
using RPM.Api.App.Queries;
using RPM.Infra.Clients;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using RPM.Domain.P2Models;
using RPM.Domain.Dto;
using System.Text.Json;
using RPM.Infra.Data.Repositories;
using System.Reflection;

namespace RPM.Api.App.Commands;

public class DeleteScheduleCommandHandler
    : IRequestHandler<DeleteScheduleCommand, bool>
{
    ICredentialQueries _credentialQueries;
    IInstanceQueries _instanceQueries;
    IInstanceJobRepository _instanceJobRepository;
    IP2Client _p2Client;
    private readonly IConfiguration _config;

    public DeleteScheduleCommandHandler(
        ICredentialQueries credentialQueries,
        IInstanceQueries instanceQueries,
        IInstanceJobRepository instanceJobRepository,
        IP2Client p2Client,
        IConfiguration config
    )
    {
        _credentialQueries = credentialQueries;
        _instanceQueries = instanceQueries;
        _instanceJobRepository = instanceJobRepository;
        _p2Client = p2Client;
        _config = config;
    }

    public async Task<bool> Handle(
        DeleteScheduleCommand request,
        CancellationToken cancellationToken
    )
    {
       _p2Client.DeleteSchedule(request.ScheduleId);
       _p2Client.DeleteJob(request.JobId);
       _instanceJobRepository.DeleteSingleInstanceJob(request.InstJobSNo);
        return true;
    }
}
