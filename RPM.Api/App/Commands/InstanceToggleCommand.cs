using MediatR;

namespace RPM.Api.App.Commands;

public class InstanceToggleCommand : IRequest<bool>
{
    public long AccountId { get; set; }
    public long InstanceId { get; set; }
    public bool PowerToggle { get; set; }
}
