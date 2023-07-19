namespace RPM.Domain.Dto;

public class InstancesJobRegisterDto
{
    public IEnumerable<long> InstIds { get; set; }
    // public long JobId { get; set; }
    public string ActionCode { get; set; }
    public string CronExpression { get; set; }
    public DateTime SavedAt { get; set; }
}
