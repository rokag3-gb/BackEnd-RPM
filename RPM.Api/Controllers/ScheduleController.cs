using Microsoft.AspNetCore.Mvc;
using RPM.Infra.Data.Repositories;
using RPM.Api.App.Queries;
using Swashbuckle.AspNetCore.Annotations;
using AutoMapper;
using MediatR;
using RPM.Infra.Clients;
using P2.API.Services.Commons;
using RPM.Api.Model;
using RPM.Domain.Dto;
using Quartz;
using Google.Api.Gax;
using P2.API.Services.Schedule;
using System.Security.Claims;

namespace RPM.Api.Controllers;

[ApiController]
[Route("account")]
public class ScheduleController : ControllerBase
{
    private readonly ILogger<ScheduleController> _logger;
    private readonly IInstanceQueries _instanceQueries;
    private readonly IInstanceRepository _instanceRepository;
    private readonly IInstanceJobQueries _instanceJobQueries;
    private readonly IAMClient _iamClient;
    private readonly IP2Client _p2Client;
    private readonly SalesClient _salesClient;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly IInstanceSnapshotQueries _instanceSnapshotQueries;

    public ScheduleController(
        ILogger<ScheduleController> logger,
        IInstanceQueries instanceQueries,
        IInstanceRepository instanceRepository,
        IInstanceJobQueries instanceJobQueries,
        IInstanceSnapshotQueries instanceSnapshotQueries,
        IAMClient iamClient,
        IP2Client p2Client,
        SalesClient salesClient,
        IMediator mediator,
        IMapper mapper
    )
    {
        _logger = logger;
        _instanceQueries = instanceQueries;
        _instanceRepository = instanceRepository;
        _instanceJobQueries = instanceJobQueries;
        _instanceSnapshotQueries = instanceSnapshotQueries;
        _iamClient = iamClient;
        _p2Client = p2Client;
        _salesClient = salesClient;
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _mapper = mapper;
    }

    [HttpGet]
    [Route("{accountId}/schedules")]
    public async Task<IEnumerable<ScheduleDto>> GetSchedules(
        [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
        [SwaggerParameter("인스턴스 ID 목록", Required = false), FromQuery]
            IEnumerable<long>? instanceIds = null
    )
    {
        var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        var instJobs = _instanceJobQueries.GetInstanceJobs(accountId, instanceIds);
        var jobIds = instJobs.Select((x) => x.JobId).ToList();
        var instIdsFromInstJobs = instJobs.Select((x) => x.InstId).ToList();
        var venderList = await _salesClient.GetKindCodeChilds(token, "VEN");
        var accCodeList = await _salesClient.GetKindCodeChilds(token, "ACC");
        var instances = _instanceQueries.GetInstancesByIds(accountId, instIdsFromInstJobs);
        var instancesJoined =
            from i in instances
            join v in venderList on i.Vendor equals v.CodeKey
            select new InstanceDto()
            {
                InstId = i.InstId,
                AccountId = i.AccountId,
                CredId = i.CredId,
                Vendor = i.Vendor,
                VendorName = v.Name,
                ResourceId = i.ResourceId,
                IsEnable = i.IsEnable,
                Name = i.Name,
                Region = i.Region,
                Type = i.Type,
                Tags = i.Tags,
                Info = i.Info,
                Note = i.Note,
                SavedAt = i.SavedAt,
                SaverId = i.SaverId
            };

        var sched = _p2Client.GetSchedules(accountId, jobIds);

        var userList = await _iamClient.ResolveUserList(
            token,
            sched.Select((x) => x.SaveUserId).ToHashSet()
        );
        var joined =
            from ij in instJobs
            join s in sched on ij.JobId equals s.JobId
            join i in instancesJoined on ij.InstId equals i.InstId
            join u in userList on s.SaveUserId equals u.Id
            join ac in accCodeList on ij.ActionCode equals ac.CodeKey
            select new ScheduleDto()
            {
                InstId = ij.InstId,
                Instance = i,
                SchId = s.SchId,
                AccountId = s.AccountId,
                JobId = s.JobId,
                Cron = s.Cron,
                IsEnable = s.IsEnable,
                ActivateDate = s.ActivateDate,
                ExpireDate = s.ExpireDate,
                Note = s.Note,
                SaveDate = s.SaveDate,
                SaveUserId = s.SaveUserId,
                SaveUserName = u.Username,
                ScheduleName = s.ScheduleName,
                SNo = ij.SNo,
                ActionCode = ij.ActionCode,
                ActionName = ac.Name,
                InstanceJobSavedAt = ij.SavedAt
            };

        return joined;
    }

    [HttpPut]
    [Route("{accountId}/schedule")]
    public async Task<ActionResult> UpdateSchedule(
        [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
        [FromQuery, SwaggerParameter("", Required = true)] long jobId,
        [FromQuery, SwaggerParameter("", Required = true)] long scheduleId,
        [FromBody] ScheduleModifyDto schedule
    )
    {
        var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _p2Client.UpdateSchedule(jobId, scheduleId, userId, schedule);
        return Ok();
    }

    [HttpDelete]
    [Route("{accountId}/schedule")]
    public async Task<IEnumerable<ScheduleDto>> DeleteSchedule(
        [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
        [FromQuery, SwaggerParameter("", Required = true)] long scheduleId
    )
    {
        var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");

        _p2Client.DeleteSchedule(accountId, scheduleId);
        return null;
    }

    [HttpGet]
    [Route("{accountId}/instance/{instanceId}/dailyScheduleSummary")]
    [SwaggerOperation("대상 인스턴스의 일별 스케줄 정보를 요약합니다")]
    public async IAsyncEnumerable<dynamic?> DailyScheduleSummary(
        [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
        [SwaggerParameter("인스턴스 ID", Required = true)] long instanceId,
        [SwaggerParameter("검색 년도", Required = true)] int year,
        [SwaggerParameter("검색 월", Required = true)] int month
    )
    {
        var snap = await _instanceSnapshotQueries.Get(accountId, instanceId, year, month);
        if (snap == null)
            yield break;

        var jobIds = await _instanceJobQueries
            .GetInstanceJobsAsync(accountId, new[] { instanceId })
            .ContinueWith(t => t.Result.Select(ij => ij.JobId));
        if (jobIds == null || jobIds.Count() <= 0)
            yield break;

        DateTime from = new DateTime(year, month, 1);
        DateTime to = new DateTime(year, month, DateTime.DaysInMonth(year, month));
        var schedules = _p2Client
            .GetSchedules(accountId, jobIds)
            .Where(s =>
            {
                var isValid = DateTime.TryParse(s.ActivateDate, out var activateDate);
                return isValid && activateDate < to;
            });
        if (schedules == null || schedules.Count() <= 0)
            yield break;

        for (int i = 1; i <= DateTime.DaysInMonth(year, month); i++)
        {
            List<dynamic> occurrenceSchedules = new List<dynamic>();
            int scheduleCount = 0;
            int occurrenceCount = 0;

            schedules = schedules.Where(s =>
            {
                var isNotExpired = true;
                if (DateTime.TryParse(s.ExpireDate, out DateTime exDate))
                    if (exDate.CompareTo(new DateTime(year, month, i)) < 0)
                        isNotExpired = false;
                return isNotExpired;
            });

            foreach (var schedule in schedules)
            {
                //if (DateTime.TryParse(schedule.ExpireDate, out DateTime exDate))
                //    if (exDate.CompareTo(new DateTime(year, month, i)) < 0)
                //        continue;

                string cron = string.Empty;
                try
                {
                    cron = CronExpressionConverter.ConvertToQuartzCronFormat(schedule.Cron);
                }
                catch
                {
                    continue;
                }

                if (CronExpression.IsValidExpression(cron) == false)
                    continue;

                var cronExpression = new Quartz.CronExpression(cron);
                var fromDate =
                    new DateTime(year, month, i, 0, 0, 0) < DateTime.Parse(schedule.ActivateDate)
                        ? DateTime.Parse(schedule.ActivateDate)
                        : new DateTime(year, month, i, 0, 0, 0);
                var toDate =
                    DateTime.TryParse(schedule.ExpireDate, out DateTime exDate)
                    && new DateTime(year, month, i, 23, 59, 59) > exDate
                        ? exDate
                        : new DateTime(year, month, i, 23, 59, 59);

                var occurrences = GetOccurrences(fromDate, toDate, cronExpression);

                if (occurrences?.Any() == false)
                    continue;

                scheduleCount++;
                var data = new
                {
                    SchId = schedule.SchId,
                    ScheduleName = schedule.ScheduleName,
                    ActivateDate = schedule.ActivateDate,
                    ExpireDate = schedule.ExpireDate,
                    CronExpression = schedule.Cron,
                    OccurrenceDate = occurrences!.Select(o => o.ToLocalTime())
                };
                occurrenceSchedules.Add(data);
                occurrenceCount += data.OccurrenceDate.Count();
            }

            if (occurrenceSchedules.Any())
                yield return new
                {
                    Date = new DateTime(year, month, i),
                    ScheduleCount = scheduleCount,
                    OccurrenceCount = occurrenceCount,
                    Schedules = occurrenceSchedules
                };
        }
    }

    private IEnumerable<DateTimeOffset> GetOccurrences(
        DateTime from,
        DateTime to,
        Quartz.CronExpression cronExpression
    )
    {
        if (from > to)
            yield break;

        var occurrence = cronExpression.GetTimeAfter(from);

        while (occurrence <= to)
        {
            yield return occurrence.Value;
            occurrence = cronExpression.GetTimeAfter(occurrence.Value);

            if (occurrence.HasValue == false)
                yield break;
        }
    }
}
