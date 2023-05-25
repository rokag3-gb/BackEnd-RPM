using MediatR;
using RPM.Api.App.Queries;
using RPM.Infra.Data.Repositories;

namespace RPM.Api.App.Commands;

public class UpdateInstancesFromCloudCommandHandler
        : IRequestHandler<UpdateInstancesFromCloudCommand>
{
    ICredentialQueries _credentialQueries;
    IInstanceRepository _instanceRepository;
    public UpdateInstancesFromCloudCommandHandler(
        ICredentialQueries credentialQueries,
        IInstanceRepository instanceRepository
    )
    {
        _credentialQueries = credentialQueries;
        _instanceRepository = instanceRepository;
    }
    public Task Handle(UpdateInstancesFromCloudCommand request, CancellationToken cancellationToken)
    {
        var credential = _credentialQueries.GetCredentialById(request.AccountId, request.CredId);
        throw new NotImplementedException();
    }
}