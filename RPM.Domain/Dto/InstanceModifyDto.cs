namespace RPM.Domain.Dto;

public class InstanceModifyDto
{
    public long AccountId { get; set; }
    public long CredId { get; set; }
    public string Vendor { get; set; }
    public string ResourceId { get; set; }
    public string Name { get; set; }
    public string Region { get; set; }
    public string Type { get; set; }
    public string Tags { get; set; }
    public string Info { get; set; }
    public string Note { get; set; }
    public string SaverId { get; set; }
}
