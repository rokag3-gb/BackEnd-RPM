namespace RPM.Domain.Dto;

public class ScheduleModifyDto
{
    public string Cron { get; set; }
    public bool IsEnable { get; set; }
    public string ActivateDate { get; set; }
    public string ExpireDate { get; set; }
    public string Note { get; set; }
    public string ScheduleName { get; set; }
}
