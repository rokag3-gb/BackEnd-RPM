using MediatR;
using RPM.Api.App.Queries;
using RPM.Infra.Clients;

namespace RPM.Api.App.Commands;

public class RegisterInstanceJobCommandHandler : IRequestHandler<RegisterInstanceJobCommand, int>
{
    ICredentialQueries _credentialQueries;
    IInstanceQueries _instanceQueries;
    IP2Client _p2Client;
    public RegisterInstanceJobCommandHandler(
        ICredentialQueries credentialQueries,
        IInstanceQueries instanceQueries,
        IP2Client p2Client
    ){
        _credentialQueries = credentialQueries;
        _instanceQueries = instanceQueries;
        _p2Client = p2Client;
    }
    public async Task<int> Handle(RegisterInstanceJobCommand request, CancellationToken cancellationToken)
    {
        var newJobId = await _p2Client.RegisterJobYaml(request.AccountId, "", request.Note, request.SavedByUserId);
        return 0;
    }
}