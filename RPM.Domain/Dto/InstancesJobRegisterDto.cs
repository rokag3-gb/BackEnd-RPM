using System.ComponentModel.DataAnnotations;

namespace RPM.Domain.Dto;

public class InstancesJobRegisterDto
{
    public IEnumerable<long> InstIds { get; set; }
    public string ScheduleName { get; set; }
    public string JobName { get; set; }
    [RegularExpression(@"ACC-TON|ACC-OFF", 
         ErrorMessage = "ACC-TON, ACC-OFF 중 하나만 사용 가능합니다.")]
    public string ActionCode { get; set; }
    public string CronExpression { get; set; }
    public DateTime ActivateDate { get; set; }
    public DateTime ExpireDate { get; set; }
    public DateTime SavedAt { get; set; }
}
 
