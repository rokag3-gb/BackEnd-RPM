namespace RPM.Domain.Dto;

public class ScheduleDto
{
    public long InstId { get; set; }
    public long SchId {get; set;}
    public long AccountId {get; set;}
    public long JobId {get; set;}
    public string Cron {get; set;}
    public string IsEnable {get; set;}
    public DateTime ActivateDate {get; set;}
    public DateTime ExpireDate {get; set;}
    public string Note {get; set;}
    public DateTime SaveDate {get; set;}
    public string SaveUserId {get; set;}
    public string ScheduleName {get; set;}
}
