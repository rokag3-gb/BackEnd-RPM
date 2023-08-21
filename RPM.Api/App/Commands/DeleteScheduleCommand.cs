using MediatR;

namespace RPM.Api.App.Commands;
public class DeleteScheduleCommand : IRequest<bool>
{
    public long ScheduleId { get; set; }
    public long JobId { get; set; }
    public long InstJobSNo { get; set; }
}
