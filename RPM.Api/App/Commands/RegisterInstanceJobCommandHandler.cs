using MediatR;
using RPM.Api.App.Queries;

namespace RPM.Api.App.Commands;

public class RegisterInstanceJobCommandHandler : IRequestHandler<RegisterInstanceJobCommand, int>
{
    ICredentialQueries _credentialQueries;
    IInstanceQueries _instanceQueries;
    public RegisterInstanceJobCommandHandler(
        ICredentialQueries credentialQueries,
        IInstanceQueries instanceQueries
    ){
        _credentialQueries = credentialQueries;
        _instanceQueries = instanceQueries;
    }
    public Task<int> Handle(RegisterInstanceJobCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}