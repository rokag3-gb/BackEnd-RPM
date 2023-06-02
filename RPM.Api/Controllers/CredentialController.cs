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

namespace RPM.Api.Controllers;

[ApiController]
[Route("account")]
public class CredentialController : ControllerBase
{
  

    private readonly ILogger<CredentialController> _logger;
    private readonly ICredentialQueries _credentialQueries;
    private readonly ICredentialRepository _credentialRepository;
    private readonly IMapper _mapper;

    public CredentialController(
        ILogger<CredentialController> logger,
        ICredentialQueries credentialQueries,
        ICredentialRepository credentialRepository,
        IMapper mapper)
    {
        _logger = logger;
        _credentialQueries = credentialQueries;
        _credentialRepository = credentialRepository;
        _mapper = mapper;
    }

    // Api for querying list of Credentials
    [HttpGet]
    [Route("{accountId}/credentials")]
    public IEnumerable<Credential> GetList(
        [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
        [SwaggerParameter("클라우드 벤더 코드(VEN-XXX 형식)", Required = false)] string? vendor,
        [SwaggerParameter("자격증명 이름 검색 키워드", Required = false)] string? credName,
        [SwaggerParameter("자격증명 사용 여부", Required = false)] bool? isEnabled = true
    )
    {
        return _credentialQueries.GetCredentials(accountId, vendor, credName, isEnabled);
    }

    [HttpGet]
    [Route("{accountId}/credential/{credId}")]
    public Credential? GetById(
        [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
        [SwaggerParameter("자격증명 ID", Required = false)] long credId
    )
    {
        return _credentialQueries.GetCredentialById(accountId, credId);
    }

    [HttpPost]
    [Route("{accountId}/credential")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Consumes(MediaTypeNames.Application.Json)]
    public ActionResult<Credential> AddCredential(
        [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
        CredentialModifyCommand credential
    )
    {
        var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var credComand = _mapper.Map<CredentialModifyDto>(credential);
        credComand.SaverId = userId;
        credComand.AccountId = accountId;
        var result = _credentialRepository.CreateSingleCredential(credComand);
        return CreatedAtAction(nameof(GetById), new { accountId = accountId, credId = result.CredId }, result);
    }

    [HttpPut]
    [Route("{accountId}/credential/{credId}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Consumes(MediaTypeNames.Application.Json)]
    public ActionResult<Credential> UpdateCredential(
        [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
        [SwaggerParameter("자격증명 ID", Required = false)] long credId,
        CredentialModifyCommand credential
    )
    {
        var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var credComand = _mapper.Map<CredentialModifyDto>(credential);
        credComand.SaverId = userId;
        credComand.AccountId = accountId;
        var result = _credentialRepository.UpdateSingleCredential(credId, credComand);
        return CreatedAtAction(nameof(GetById), new { accountId = accountId, credId = result.CredId }, result);
    }

    [HttpDelete]
    [Route("{accountId}/credential/{credId}")]
    public ActionResult DeleteById(
        [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
        [SwaggerParameter("자격증명 ID", Required = false)] long credId
    )
    {
        _credentialRepository.DeleteSingleCredential(accountId, credId);
        return Ok();
    }

    [HttpDelete]
    [Route("{accountId}/credentials")]
    public ActionResult DeleteMultipleById(
        [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
        [FromQuery, SwaggerParameter("자격증명 ID", Required = false)] List<long> credId
    )
    {
        var affectedRows = _credentialRepository.DeleteMultipleCredentials(accountId, credId);
        return Ok(new AffectedRowsDto { AffectedRows = affectedRows });
    }
}
