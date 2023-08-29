using MediatR;

namespace RPM.Api.App.Commands;

public class InstanceToggleCommand : IRequest<IEnumerable<long>>
{
    public long AccountId { get; set; }
    public long InstanceId { get; set; }
    public string ActionCode { get; set; }
}
