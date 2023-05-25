using MediatR;

namespace RPM.Api.App.Commands;

public class UpdateInstancesFromCloudCommand : IRequest
{
    public long CredId { get; set; }
    public long AccountId { get; set; }

}