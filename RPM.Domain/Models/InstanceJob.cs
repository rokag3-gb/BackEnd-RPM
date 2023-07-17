namespace RPM.Domain.Models;

public class InstanceJob
{
    public long SNo { get; set; }
    public long InstId { get; set; }
    public long JobId { get; set; }
    public string ActionCode { get; set; }
    public DateTime SavedAt { get; set; }
}
