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
using RPM.Api.Model;

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
    private readonly InstanceCostCalculator _instanceCostCalculator;
    private readonly SalesClient _salesClient;

    public InstanceController(
        ILogger<InstanceController> logger,
        IInstanceQueries instanceQueries,
        ICredentialQueries credentialQueries,
        IInstanceRepository instanceRepository,
        IAMClient iamClient,
        IMediator mediator,
        IMapper mapper,
        InstanceCostCalculator instanceCostCalculator,
        SalesClient salesClient
    )
    {
        _logger = logger;
        _instanceQueries = instanceQueries;
        _credentialQueries = credentialQueries;
        _instanceRepository = instanceRepository;
        _iamClient = iamClient;
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _mapper = mapper;
        _instanceCostCalculator = instanceCostCalculator;
        _salesClient = salesClient;
    }

    // Api for querying list of Credentials
    [HttpGet]
    [Route("{accountId}/instances")]
    public async Task<IEnumerable<dynamic>> GetList(
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
            userList = new List<UserListItem>();

        var venderList = await _salesClient.GetKindCodeChilds(token, "VEN");

        if (venderList == null)
            venderList = new List<Code>();

        // instances와 venderList를 조인
        var joinedInstances = instances
            .Join(venderList, i => i.Vendor?.ToString(), c => c.CodeKey?.ToString(), (i, c) => new { i, c })
            ;

        var result =
            from i in joinedInstances
            join userRaw in userList on i.i.SaverId equals userRaw.Id into joinedUsers
            from user in joinedUsers.DefaultIfEmpty()
            select new
            {
                InstId = i.i.InstId,
                AccountId = i.i.AccountId,
                CredId = i.i.CredId,
                Vendor = i.i.Vendor,
                VendorName = i.c?.Name ?? "",
                ResourceId = i.i.ResourceId,
                IsEnable = i.i.IsEnable,
                Name = i.i.Name,
                Region = i.i.Region,
                Type = i.i.Type,
                Tags = i.i.Tags,
                Info = i.i.Info,
                Note = i.i.Note,
                SavedAt = i.i.SavedAt,
                SaverId = i.i.SaverId,
                SaverName = user?.Username ?? ""
            };

        /*
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
                SavedAt = instance.SavedAt,
                SaverId = instance.SaverId,
                SaverName = user?.Username ?? ""
            };
        */
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
                JobName = registerParams.JobName,
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
            SavedAt = instance.SavedAt,
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
    public async IAsyncEnumerable<dynamic> DailyCost([SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
                                                                    [SwaggerParameter("인스턴스 ID", Required = true)] long instanceId,
                                                                    [SwaggerParameter("검색 년도", Required = true)] int year,
                                                                    [SwaggerParameter("검색 월", Required = true)] int month)
    {
        var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        await foreach(var cost in _instanceCostCalculator.DailyCost(accountId, instanceId, year, month, token))
        {
            yield return cost;
        }
    }


    [HttpGet]
    [Route("{accountId}/instance/{instanceId}/monthlyCost")]
    [SwaggerOperation("대상 년월로부터 최근 12개월 간 인스턴스의 월간 사용금액을 조회합니다.")]
    public async IAsyncEnumerable<InstanceCostDto> MonthlyCost([SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
                                                               [SwaggerParameter("인스턴스 ID", Required = true)] long instanceId,
                                                               [SwaggerParameter("검색 년도", Required = true)] int year,
                                                               [SwaggerParameter("검색 월", Required = true)] int month)
    {
        var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");

        var dateList = new List<DateTime>();
        var to = new DateTime(year, month, 1);

        for (int i = 11; i >= 0; i--)
        {
            dateList.Add(to.AddMonths(-i));
        }

        await foreach (var instanceCost in _instanceCostCalculator.InstanceCostPerMonth(accountId, new[] { instanceId }, dateList, token))
        {
            yield return instanceCost.First();
        }
    }
}
