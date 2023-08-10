using Microsoft.AspNetCore.Mvc;
using RPM.Infra.Data.Repositories;
using RPM.Api.App.Queries;
using RPM.Domain.Dto;
using RPM.Api.App.Commands;
using RPM.Domain.Models;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Net.Mime;
using System.Security.Claims;
using AutoMapper;
using MediatR;
using RPM.Infra.Clients;
using System.Text.Json;
using Azure.Identity;
using P2.API.Services.Commons;
using System.Diagnostics;

namespace RPM.Api.Controllers;

[ApiController]
[Route("account")]
public class InstanceController : ControllerBase
{
    private readonly ILogger<InstanceController> _logger;
    private readonly IInstanceQueries _instanceQueries;
    private readonly ICredentialQueries _credentialQueries;
    private readonly IInstanceRepository _instanceRepository;
    private readonly IAMClient _iamClient;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly IInstanceJobQueries _instanceJobQueries;
    private readonly IInstancePriceQueries _instancePriceQueries;
    private readonly IInstanceSnapshotQueries _instanceSnapshotQueries;
    private readonly IP2Client _p2Client;

    public InstanceController(
        ILogger<InstanceController> logger,
        IInstanceQueries instanceQueries,
        ICredentialQueries credentialQueries,
        IInstanceRepository instanceRepository,
        IAMClient iamClient,
        IMediator mediator,
        IMapper mapper,
        IInstanceJobQueries instanceJobQueries,
        IInstancePriceQueries instancePriceQueries,
        IInstanceSnapshotQueries instanceSnapshotQueries,
        IP2Client p2Client
    )
    {
        _logger = logger;
        _instanceQueries = instanceQueries;
        _credentialQueries = credentialQueries;
        _instanceRepository = instanceRepository;
        _iamClient = iamClient;
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _mapper = mapper;
        _instanceJobQueries = instanceJobQueries;
        _instancePriceQueries = instancePriceQueries;
        _instanceSnapshotQueries = instanceSnapshotQueries;
        _p2Client = p2Client;
    }

    // Api for querying list of Credentials
    [HttpGet]
    [Route("{accountId}/instances")]
    public async Task<IEnumerable<InstanceDto>> GetList(
        [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
        [SwaggerParameter("클라우드 벤더 코드(VEN-XXX 형식)", Required = false)] string? vendor,
        [SwaggerParameter("리소스 ID", Required = false)] string? resourceId,
        [SwaggerParameter("리소스 이름", Required = false)] string? name,
        [SwaggerParameter("리전 코드", Required = false)] string? region,
        [SwaggerParameter("리소스 유형", Required = false)] string? type
    )
    {
        var instances = _instanceQueries.GetInstances(
            accountId,
            null,
            vendor,
            resourceId,
            name,
            region,
            type
        );
        var saverIdsSet = instances.Select(x => x.SaverId).ToHashSet();
        var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        var userList = await _iamClient.ResolveUserList(token, saverIdsSet);
        if (userList == null)
        {
            userList = new List<UserListItem>();
        }

        var result =
            from instance in instances
            join userRaw in userList on instance.SaverId equals userRaw.Id into joinedUsers
            from user in joinedUsers.DefaultIfEmpty()
            select new InstanceDto
            {
                InstId = instance.InstId,
                AccountId = instance.AccountId,
                CredId = instance.CredId,
                Vendor = instance.Vendor,
                ResourceId = instance.ResourceId,
                IsEnable = instance.IsEnable,
                Name = instance.Name,
                Region = instance.Region,
                Type = instance.Type,
                Tags = instance.Tags,
                Info = instance.Info,
                Note = instance.Note,
                SaverId = instance.SaverId,
                SaverName = user?.Username ?? ""
            };
        return result;
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("{accountId}/instances/registerWithJob")]
    public async Task<ActionResult<IEnumerable<long>>> RegisterInstancesWithJob(
        [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
        [FromBody] InstancesJobRegisterDto registerParams
    )
    {
        var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var result = await _mediator.Send(
            new RegisterInstanceJobCommand()
            {
                AccountId = accountId,
                ScheduleName = registerParams.ScheduleName,
                InstanceIds = registerParams.InstIds,
                ActionCode = registerParams.ActionCode,
                Note = "",
                SavedByUserId = userId ?? "",
                ActivateDate = registerParams.ActivateDate,
                ExpireDate = registerParams.ExpireDate,
                CronExpressioon = registerParams.CronExpression
            }
        );
        if (result != null)
        {
            return Ok(result);
        }
        return StatusCode(500);
    }

    [HttpGet]
    [Route("{accountId}/instance/{instanceId}")]
    [SwaggerResponse(404, "ID 에 해당하는 인스턴가 없음")]
    public async Task<ActionResult<InstanceDto?>> GetById(
        [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
        [SwaggerParameter("인스턴스 ID", Required = false)] long instanceId
    )
    {
        var instance = _instanceQueries.GetInstanceById(accountId, instanceId);
        if (instance == null)
        {
            return NotFound();
        }
        var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        var user = await _iamClient.ResolveUser(token, instance.SaverId);
        return new InstanceDto()
        {
            InstId = instance.InstId,
            AccountId = instance.AccountId,
            CredId = instance.CredId,
            Vendor = instance.Vendor,
            ResourceId = instance.ResourceId,
            IsEnable = instance.IsEnable,
            Name = instance.Name,
            Region = instance.Region,
            Type = instance.Type,
            Tags = instance.Tags,
            Info = instance.Info,
            Note = instance.Note,
            SaverId = instance.SaverId,
            SaverName = user?.Username ?? ""
        };
    }

    [HttpGet]
    [Route("{accountId}/instance/{instanceId}/status")]
    [SwaggerResponse(404, "ID 에 해당하는 인스턴가 없음")]
    public async Task<ActionResult<InstancesStatusDto>> GetInstanceStatusById(
        [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
        [SwaggerParameter("인스턴스 ID", Required = false)] long instanceId
    )
    {
        var instance = _instanceQueries.GetInstanceById(accountId, instanceId);
        if (instance == null)
        {
            return NotFound();
        }
        var credential = _credentialQueries.GetCredentialById(accountId, instance.CredId);
        var credData = JsonSerializer.Deserialize<JsonElement>(credential.CredData);
        var vmStatus = new InstancesStatusDto();
        switch (instance.Vendor)
        {
            case "VEN-AWS":
                var aws = new AWSClient(
                    credData.GetProperty("access_key_id").GetString(),
                    credData.GetProperty("access_key_secret").GetString()
                );
                vmStatus = await aws.GetAwsVMStatus(
                    credData.GetProperty("region_code").GetString(),
                    instance.ResourceId
                );
                break;
            case "VEN-AZT":
                var azure = new AzureClient(
                    new ClientSecretCredential(
                        credData.GetProperty("tenant_id").GetString(),
                        credData.GetProperty("client_id").GetString(),
                        credData.GetProperty("client_secret").GetString()
                    )
                );
                var instanceInfo = JsonSerializer.Deserialize<JsonElement>(instance.Info);
                vmStatus = await azure.GetAzureVMStatus(
                    instanceInfo.GetProperty("Id").GetProperty("ResourceGroupName").GetString(),
                    instance.Name
                );
                break;
            case "VEN-GCP":
                var gcp = new GoogleCloudClient(credential.CredData);
                vmStatus = await gcp.GetGcloudComputeEngineStatus(instance.Region, instance.ResourceId);
                break;
        }
        return vmStatus;
    }

    [HttpPut]
    [Route("{accountId}/instance/{instanceId}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Consumes(MediaTypeNames.Application.Json)]
    public ActionResult<Credential> UpdateInstanceNote(
        [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
        [SwaggerParameter("인스턴스 ID", Required = false)] long instanceId,
        InstanceNoteModifyCommand instance
    )
    {
        var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var instDto = _mapper.Map<InstanceNoteModifyDto>(instance);
        instDto.SaverId = userId ?? "";
        instDto.AccountId = accountId;
        var result = _instanceRepository.UpdateSingleInstanceNote(instanceId, instDto);
        return CreatedAtAction(
            nameof(GetById),
            new { accountId = accountId, instanceId = result.InstId },
            result
        );
    }

    [HttpDelete]
    [Route("{accountId}/instance/{instanceId}")]
    [SwaggerResponse(404, "ID 에 해당하는 인스턴가 없어 삭제할 수 없음")]
    public ActionResult DeleteById(
        [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
        [SwaggerParameter("인스턴스 ID", Required = false)] long instanceId
    )
    {
        var result = _instanceRepository.DeleteSingleInstance(accountId, instanceId);

        if (result == 0)
        {
            return NotFound();
        }
        return Ok();
    }

    [HttpPost]
    [Route("{accountId}/instance/fetchWithCredential/{credId}")]
    [Route("{accountId}/credential/{credId}/fetchInstances")]
    [SwaggerResponse(404, "ID 에 해당하는 인스턴가 없어 삭제할 수 없음")]
    public async Task<ActionResult> FetchWithCredential(
        [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
        [SwaggerParameter("자격증명 ID", Required = false)] long credId
    )
    {
        var result = await _mediator.Send(
            new UpdateInstancesFromCloudCommand() { CredId = credId, AccountId = accountId }
        );

        return Ok(new AffectedRowsDto() { AffectedRows = result });
    }

    [HttpGet]
    [Route("{accountId}/instance/{instanceId}/dailyCost")]
    [SwaggerOperation("대상 인스턴스의 한달 간 일별 사용 금액을 조회합니다.")]
    public async Task<ActionResult<IEnumerable<dynamic>>> DailyCost([SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
                                                                    [SwaggerParameter("인스턴스 ID", Required = true)] long instanceId,
                                                                    [SwaggerParameter("검색 년도", Required = true)] int year,
                                                                    [SwaggerParameter("검색 월", Required = true)] int month)
    {
        var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        var startMonthDate = new DateTime(year, month, 1);
        var startPreviousMonthDate = startMonthDate.AddMonths(-1);
        var endMonthDate = startMonthDate.AddMonths(1);
        var monthSpan = endMonthDate.Subtract(startMonthDate);

        var snap = await _instanceSnapshotQueries.Get(accountId, instanceId, year, month);
        if (snap == null)
        {
            _logger.LogInformation($"해당 리소스(account id - {accountId}, inst id - {instanceId})에 대한 RPM 워크플로가 없음.");
            return Ok(new List<dynamic>());
        }

        var price = await _instancePriceQueries.Get(new[] { instanceId }).ContinueWith(t => t.Result.FirstOrDefault());
        if (price == null)
        {
            _logger.LogError($"가격 정보를 찾을 수 없습니다. (account id - {accountId}, inst id - {instanceId})");
            return StatusCode(500);
        }

        var instanceJobs = await _instanceJobQueries.GetInstanceJobsAsync(accountId, new[] { instanceId });
        if (instanceJobs == null)
            return Ok(); //todo. cost max

        //var instance = _instanceQueries.GetInstanceById(accountId, instanceId);
        var runs = await _p2Client.GetRuns(instanceJobs.Select(ij => ij.JobId),
                                           startMonthDate,
                                           new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59),
                                           new[] { RunState.Success },
                                           token);

        var dt = new DateTime(startPreviousMonthDate.Year,
                              startPreviousMonthDate.Month,
                              DateTime.DaysInMonth(startPreviousMonthDate.Year, startPreviousMonthDate.Month),
                              23,
                              59,
                              59);
        var latestRunActionCode = await _p2Client.GetLatest(instanceJobs.Select(ij => ij.JobId),
                                                  null,
                                                  null,
                                                  dt,
                                                  "RUN-SUC",
                                                  token).ContinueWith<string?>(t =>
                                                  {
                                                      if (t.Status >= TaskStatus.Canceled)
                                                          return null;
                                                      if (t.Result == null)
                                                          return null;

                                                      return instanceJobs.FirstOrDefault(i => i.JobId == t.Result.JobId)?.ActionCode;
                                                  });

        var instanceJobAndRun = instanceJobs.Join(runs,
                                                  ij => ij.JobId,
                                                  r => r.JobId,
                                                  (ij, r) => (instanceId: ij.InstId, actionCode: ij.ActionCode, runDate: DateTime.Parse(r.CompletedDate))).ToList();

        if (latestRunActionCode == "ACT-OFF")
            instanceJobAndRun.Insert(0, (instanceId, "ACT-OFF", startMonthDate));
        else
            instanceJobAndRun.Insert(0, (instanceId, "ACT-TON", startMonthDate));
        instanceJobAndRun.Add((instanceId, "ACT-OFF", startMonthDate.AddMonths(1)));
        instanceJobAndRun = instanceJobAndRun.OrderBy(ijr => ijr.runDate).ToList();

        DateTime? activePeriodFrom = null;
        List<(int Day, TimeSpan RunningTime)> runningTimes = new List<(int Day, TimeSpan RunningTime)>();
        for (int i = 0; i < instanceJobAndRun.Count(); i++)
        {
            if (instanceJobAndRun[i].actionCode == "ACT-TON")
            {
                if (activePeriodFrom != null)
                    continue;
                activePeriodFrom = instanceJobAndRun[i].runDate;
            }
            else
            {
                if (activePeriodFrom == null)
                    continue;

                TimeSpan activePeriod = TimeSpan.Zero;
                if (activePeriodFrom.Value.Day != instanceJobAndRun[i].runDate.Day)
                {
                    activePeriod = activePeriodFrom.Value.AddDays(1).Date.Subtract(activePeriodFrom.Value);
                    instanceJobAndRun.Insert(i + 1, (instanceId, "ACT-TON", activePeriodFrom.Value.AddDays(1).Date));
                    instanceJobAndRun.Insert(i + 2, (instanceId, "ACT-OFF", instanceJobAndRun[i].runDate));
                }
                else
                    activePeriod = instanceJobAndRun[i].runDate.Subtract(activePeriodFrom.Value);
                if (activePeriod.TotalMilliseconds <= 0)
                {
                    activePeriodFrom = null;
                    continue;
                }

                Debug.WriteLine($"{instanceId} : active period - {activePeriod.TotalHours}");

                runningTimes.Add((activePeriodFrom.Value.Day, activePeriod));
                activePeriodFrom = null;
            }
        }

        List<dynamic> dailyCosts = new List<dynamic>();
        TimeSpan remainRunningTime = TimeSpan.Zero;
        for (int i = 1; i <= DateTime.DaysInMonth(year, month); i++)
        {
            var runsByDay = runningTimes.Where(r => r.Day == i);
            if (runsByDay.Count() <= 0 && remainRunningTime == TimeSpan.Zero)
            {
#if DEBUG
                dailyCosts.Add(new { Day = $"{year}-{month}-{i}", Cost_krw = 0, Hours = remainRunningTime });
#else
                dailyCosts.Add(new { Day = $"{year}-{month}-{i}", Cost_krw = 0 });
#endif
                continue;
            }
            
            foreach (var runningTime in runsByDay)
            {
                remainRunningTime = remainRunningTime.Add(runningTime.RunningTime);
            }

            if (remainRunningTime != TimeSpan.Zero)
            {
                var comp = remainRunningTime.CompareTo(TimeSpan.FromHours(24));
                if (comp >= 0)
                {
#if DEBUG
                    dailyCosts.Add(new { Day = $"{year}-{month}-{i}", Cost_krw = TimeSpan.FromHours(24).TotalHours * price.Price_KRW, Hours = remainRunningTime });
#else
                    dailyCosts.Add(new { Day = $"{year}-{month}-{i}", Cost_krw = TimeSpan.FromHours(24).TotalHours * price.Price_KRW });
#endif
                    remainRunningTime = remainRunningTime.Subtract(TimeSpan.FromHours(24));
                }
                else
                {
#if DEBUG
                    dailyCosts.Add(new { Day = $"{year}-{month}-{i}", Cost_krw = remainRunningTime.TotalHours * price.Price_KRW, Hours = remainRunningTime });
#else
                    dailyCosts.Add(new { Day = $"{year}-{month}-{i}", Cost_krw = remainRunningTime.TotalHours * price.Price_KRW });
#endif
                    remainRunningTime = TimeSpan.Zero;
                }
            }
        }

        return Ok(dailyCosts);
    }
}
