using MediatR;

namespace RPM.Api.App.Commands;

public class RegisterInstanceJobCommand : IRequest<IEnumerable<long>>
{
    public long AccountId { get; set; }
    public IEnumerable<long> InstanceIds { get; set; }
    public string ScheduleName { get; set; }
    public string JobName { get; set; }
    public string CronExpressioon { get; set; }
    public string ActionCode { get; set; }
    public DateTime ActivateDate { get; set; }
    public DateTime ExpireDate { get; set; }
    public string Note { get; set; }
    public string SavedByUserId { get; set; }
}
