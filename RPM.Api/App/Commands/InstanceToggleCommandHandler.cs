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

public class InstanceToggleCommandHandler
    : IRequestHandler<InstanceToggleCommand, IEnumerable<long>>
{
    ICredentialQueries _credentialQueries;
    IInstanceQueries _instanceQueries;
    private readonly IConfiguration _config;

    public InstanceToggleCommandHandler(
        ICredentialQueries credentialQueries,
        IInstanceQueries instanceQueries,
        IConfiguration config
    )
    {
        _credentialQueries = credentialQueries;
        _instanceQueries = instanceQueries;
        _config = config;
    }

    public async Task<IEnumerable<long>> Handle(
        InstanceToggleCommand request,
        CancellationToken cancellationToken
    )
    {
        // VM, Credential 목록 쿼리
        var instance = _instanceQueries.GetInstanceById(
            request.AccountId,
            request.InstanceId
        );
        var credential = _credentialQueries.GetCredentialById(
            request.AccountId,
            instance.CredId
        );
      
        return null;
    }
}
