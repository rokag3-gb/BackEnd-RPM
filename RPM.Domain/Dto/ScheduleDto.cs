namespace RPM.Domain.Dto;

public class ScheduleDto
{
    public long InstId { get; set; }
    public string InstName { get; set; }
    public long SchId {get; set;}
    public long AccountId {get; set;}
    public long JobId {get; set;}
    public string Cron {get; set;}
    public bool IsEnable {get; set;}
    public string ActivateDate {get; set;}
    public string ExpireDate {get; set;}
    public string Note {get; set;}
    public string SaveDate {get; set;}
    public string SaveUserId {get; set;}
    public string ScheduleName {get; set;}
}
