using MediatR;

namespace RPM.Api.App.Commands;

public class RegisterInstanceJobCommand : IRequest<int>
{
    
    public long AccountId { get; set; }
    public IEnumerable<long> InstanceIds { get; set; }
    public string CronExpressioon { get; set; }
    public string ActionCode { get; set; }
    public string Note { get; set; }
    public string SavedByUserId { get; set; }

}