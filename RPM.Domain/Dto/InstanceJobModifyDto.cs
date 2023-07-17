namespace RPM.Domain.Dto;

public class InstanceJobModifyDto
{
    public long InstId { get; set; }
    public long JobId { get; set; }
    public string ActionCode { get; set; }
    public DateTime SavedAt { get; set; }
}
