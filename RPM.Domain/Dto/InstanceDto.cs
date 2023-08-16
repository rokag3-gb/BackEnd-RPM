namespace RPM.Domain.Dto;

public class InstanceDto
{
    public long InstId { get; set; }
    public long AccountId { get; set; }
    public long CredId { get; set; }
    public string Vendor { get; set; }
    public string ResourceId { get; set; }
    public bool IsEnable { get; set; }
    public string Name { get; set; }
    public string Region { get; set; }
    public string Type { get; set; }
    public string Tags { get; set; }
    public string Info { get; set; }
    public string Note { get; set; }
    public DateTime SavedAt { get; set; }
    public string SaverId { get; set; }
    public string SaverName { get; set;}
}