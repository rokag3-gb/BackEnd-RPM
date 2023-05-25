namespace RPM.Domain.Models;

public class Credential
{
    public long CredId { get; set; }
    public long AccountId { get; set; }
    public string Vendor { get; set; }
    public string CredName { get; set; }
    public bool IsEnabled { get; set; }
    public string CredData { get; set; }
    public string Note { get; set; }
    public DateTime SavedAt { get; set; }
    public string SaverId { get; set; }
}
