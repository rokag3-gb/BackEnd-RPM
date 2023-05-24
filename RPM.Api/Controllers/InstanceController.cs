using Microsoft.AspNetCore.Mvc;
using RPM.Api.App.Repository;
using RPM.Api.App.Queries;
using RPM.Domain.Dto;
using RPM.Domain.Commands;
using RPM.Domain.Models;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Net.Mime;
using System.Security.Claims;
using AutoMapper;

namespace RPM.Api.Controllers;

[ApiController]
[Route("account")]
public class InstanceController : ControllerBase
{
  
    private readonly ILogger<InstanceController> _logger;
    private readonly IInstanceQueries _instanceQueries;
    private readonly IInstanceRepository _instanceRepository;
    private readonly IMapper _mapper;

    public InstanceController(
        ILogger<InstanceController> logger,
        IInstanceQueries instanceQueries,
        IInstanceRepository instanceRepository,
        IMapper mapper)
    {
        _logger = logger;
        _instanceQueries = instanceQueries;
        _instanceRepository = instanceRepository;
        _mapper = mapper;
    }

    // Api for querying list of Credentials
    [HttpGet]
    [Route("{accountId}/instances")]
    public IEnumerable<Instance> GetList(
        [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
        [SwaggerParameter("클라우드 벤더 코드(VEN-XXX 형식)", Required = false)] string? vendor,
        [SwaggerParameter("리소스 ID", Required = false)] string? resourceId,
        [SwaggerParameter("리소스 이름", Required = false)] string? name,
        [SwaggerParameter("리전 코드", Required = false)] string? region,
        [SwaggerParameter("리소스 유형", Required = false)] string? type
    )
    {
        return _instanceQueries.GetInstances(accountId, vendor, resourceId, name, region, type);
    }

    [HttpGet]
    [Route("{accountId}/instance/{instanceId}")]
    public ActionResult<Instance?> GetById(
        [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
        [SwaggerParameter("인스턴스 ID", Required = false)] long instanceId
    )
    {
        var result =  _instanceQueries.GetInstanceById(accountId, instanceId);
        if (result == null)
        {
            return NotFound();
        }
        return result;
    }

    // [HttpPost]
    // [Route("{accountId}/instance")]
    // [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    // [Consumes(MediaTypeNames.Application.Json)]
    // public ActionResult<Credential> AddCredential(
    //     [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
    //     CredentialModifyDto credential
    // )
    // {
    //     var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    //     var credComand = _mapper.Map<CredentialModifyCommand>(credential);
    //     credComand.SaverId = userId;
    //     credComand.AccountId = accountId;
    //     var result = _instanceRepository.CreateSingleCredential(credComand);
    //     return CreatedAtAction(nameof(GetById), new { accountId = accountId, credId = result.CredId }, result);
    // }

    // [HttpPut]
    // [Route("{accountId}/instance/{credId}")]
    // [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    // [Consumes(MediaTypeNames.Application.Json)]
    // public ActionResult<Credential> UpdateCredential(
    //     [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
    //     [SwaggerParameter("자격증명 ID", Required = false)] long credId,
    //     CredentialModifyDto credential
    // )
    // {
    //     var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    //     var credComand = _mapper.Map<CredentialModifyCommand>(credential);
    //     credComand.SaverId = userId;
    //     credComand.AccountId = accountId;
    //     var result = _instanceRepository.UpdateSingleCredential(credId, credComand);
    //     return CreatedAtAction(nameof(GetById), new { accountId = accountId, credId = result.CredId }, result);
    // }

    [HttpDelete]
    [Route("{accountId}/instance/{instanceId}")]
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
}
