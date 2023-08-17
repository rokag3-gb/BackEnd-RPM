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
        [SwaggerParameter("검색종료일시", Required = true), FromQuery] DateTime? periodTo
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
        var runs = await _p2Client.GetRunListByJobIds(jobIds, accountId, periodFrom, periodTo, token);

        // #ACC 코드 가져오기
        var actionCode = await _salesClient.GetKindCodeChilds(token, "ACC");

        if (actionCode is null)
            actionCode = new List<Code>();

        // 데이터 결합하여 동적인 IEnumerable 생성
        var result = runs.Runs
            .Join(instJobs
                    , r => r.JobId, i => i.JobId
                    , (r, i) => new { r, i })
            .Join(actionCode
                    , r => r.i.ActionCode, c => c.CodeKey
                    , (r, c) => new
                    {
                        r.r.RunId,
                        r.r.AccountId,
                        r.r.StartedDate,
                        r.r.RunStateCode,
                        r.r.RunStateName,
                        r.r.CompletedDate,
                        r.r.HostInfo,
                        r.r.JobId,
                        r.r.JobName,
                        r.r.SchId,
                        r.r.ScheduleName,
                        r.r.DurationMilliseconds,
                        r.i.SNo,
                        r.i.InstId,
                        r.i.ActionCode,
                        ActionName = c?.Name,
                        r.i.SavedAt,
                    });

        return result;
    }
}