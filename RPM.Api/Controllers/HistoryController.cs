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
using P2.API.Services.Run;
using Microsoft.IdentityModel.Tokens;
using P2.API.Services.Commons;

namespace RPM.Api.Controllers;

[ApiController]
[Route("account")]
public class HistoryController : ControllerBase
{
    private readonly ILogger<HistoryController> _logger;
    private readonly IInstanceQueries _instanceQueries;
    private readonly IInstanceJobQueries _instanceJobQueries;
    private readonly ICredentialQueries _credentialQueries;
    private readonly IInstanceRepository _instanceRepository;
    private readonly IAMClient _iamClient;
    private readonly IP2Client _p2Client;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly InstanceCostCalculator _instanceCostCalculator;
    private readonly SalesClient _salesClient;

    public HistoryController(
        ILogger<HistoryController> logger,
        IInstanceQueries instanceQueries,
        IInstanceJobQueries instanceJobQueries,
        ICredentialQueries credentialQueries,
        IInstanceRepository instanceRepository,
        IAMClient iamClient,
        IP2Client p2Client,
        IMediator mediator,
        IMapper mapper,
        InstanceCostCalculator instanceCostCalculator,
        SalesClient salesClient
    )
    {
        _logger = logger;
        _instanceQueries = instanceQueries;
        _instanceJobQueries = instanceJobQueries;
        _credentialQueries = credentialQueries;
        _instanceRepository = instanceRepository;
        _iamClient = iamClient;
        _p2Client = p2Client;
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _mapper = mapper;
        _instanceCostCalculator = instanceCostCalculator;
        _salesClient = salesClient;
    }

    [HttpGet]
    [Route("{accountId}/history")]
    [SwaggerOperation("특정 Instance_Job에 해당하는 P2 History를 조회합니다.")]
    public async Task<IEnumerable<dynamic>> GetHistory(
        [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
        [SwaggerParameter("대상 인스턴스 ID(s)", Required = true), FromQuery] List<long> instanceIds,
        [SwaggerParameter("검색시작일시", Required = true), FromQuery] DateTime? periodFrom,
        [SwaggerParameter("검색종료일시", Required = true), FromQuery] DateTime? periodTo,
        [SwaggerParameter("Offset, Limit 을 둘다 제공하면 작동합니다.", Required = false), FromQuery] long? offset,
        [SwaggerParameter("Offset, Limit 을 둘다 제공하면 작동합니다.", Required = false), FromQuery] long? limit
        )
    {
        #region (주석처리) 빠른 테스트를 위한 변수 강제 초기화
        //if (accountId is null || accountId <= 0)
        //    accountId = 1;

        //if (instanceIds is null || instanceIds.Count <= 0)
        //    instanceIds.Add(10);

        //if (periodFrom is null)
        //    periodFrom = Convert.ToDateTime("2023-07-01 09:00");

        //if (periodTo is null)
        //    periodTo = Convert.ToDateTime("2023-08-31 23:59");
        #endregion

        var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");

        // Instance Ids 로 JobIds 구하기
        var instJobs = _instanceJobQueries.GetInstanceJobs(accountId, instanceIds);
        var jobIds = instJobs.Select((x) => x.JobId).ToList();

        // P2 통해서 Run 데이터 가져오기
        var runs = await _p2Client.GetRunListByJobIds(jobIds, accountId, periodFrom, periodTo, offset, limit, token);

        // #ACC 코드 가져오기
        var actionCode = await _salesClient.GetKindCodeChilds(token, "ACC");

        if (actionCode is null)
            actionCode = new List<Code>();

        // Run과 instJobs를 inner join하여 IEnumerable1 생성
        var IEnumerable1 = runs.Runs
            .Join(instJobs
                    , r => r.JobId, i => i.JobId
                    , (r, i) => new { r, i });

        // IEnumerable1에 ActionCode 를 left outer join하여 result 생성
        var result = from run in IEnumerable1
                      join code in actionCode on run.i.ActionCode equals code.CodeKey into d
                      from outer in d.DefaultIfEmpty()
                      select new {
                          run.r.RunId,
                          run.r.AccountId,
                          run.r.StartedDate,
                          run.r.RunStateCode,
                          run.r.RunStateName,
                          run.r.CompletedDate,
                          run.r.HostInfo,
                          run.r.JobId,
                          run.r.JobName,
                          run.r.SchId,
                          run.r.ScheduleName,
                          run.r.DurationMilliseconds,
                          run.i.SNo,
                          run.i.InstId,
                          run.i.ActionCode,
                          ActionName = outer?.Name,
                          run.i.SavedAt,
                      };

        return result;
    }
}