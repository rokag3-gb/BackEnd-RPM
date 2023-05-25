namespace RPM.Api.App.Commands;

public class CredentialModifyCommand
{
    public string Vendor { get; set; }
    public string CredName { get; set; }
    public bool IsEnabled { get; set; }
    public string CredData { get; set; }
    public string Note { get; set; }
}
