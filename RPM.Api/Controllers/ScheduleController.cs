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
using P2.API.Services.Commons;

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
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public ScheduleController(
        ILogger<ScheduleController> logger,
        IInstanceQueries instanceQueries,
        IInstanceRepository instanceRepository,
        IInstanceJobQueries instanceJobQueries,
        IAMClient iamClient,
        IP2Client p2Client,
        IMediator mediator,
        IMapper mapper
    )
    {
        _logger = logger;
        _instanceQueries = instanceQueries;
        _instanceRepository = instanceRepository;
        _instanceJobQueries = instanceJobQueries;
        _iamClient = iamClient;
        _p2Client = p2Client;
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _mapper = mapper;
    }

   

    [HttpPost]
    [Route("{accountId}/schedules")]
    public async Task<IEnumerable<JobScheduleData>> GetSchedules(
        [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
        [SwaggerParameter("인스턴스 ID 목록", Required = false), FromQuery] IEnumerable<long>? instanceIds
    )
    {
        var instJobs = _instanceJobQueries.GetInstanceJobs(accountId, instanceIds);
        var result = new List<JobScheduleData>();
        foreach (var instJob in instJobs){
            var sched =  _p2Client.GetSchedules(
                instJob.JobId
            );
            result.AddRange(sched);
        }
        return result;
    }
}
