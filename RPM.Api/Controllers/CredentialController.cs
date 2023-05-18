using Microsoft.AspNetCore.Mvc;
using RPM.Api.App;
using RPM.Api.App.Repository;
using RPM.Api.App.Queries;
using RPM.Domain.Dto;
using RPM.Domain.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace RPM.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class CredentialController : ControllerBase
{
  

    private readonly ILogger<CredentialController> _logger;
    private readonly ICredentialQueries _credentialQueries;
    private readonly ICredentialRepository _credentialRepository;

    public CredentialController(
        ILogger<CredentialController> logger,
        ICredentialQueries credentialQueries,
        ICredentialRepository credentialRepository)
    {
        _logger = logger;
        _credentialQueries = credentialQueries;
        _credentialRepository = credentialRepository;
    }

    // Api for querying list of Credentials
    [HttpGet]
    [Route("{accountId}")]
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
    [Route("{accountId}/{credId}")]
    public Credential? GetById(
        [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
        [SwaggerParameter("자격증명 사용 여부", Required = false)] long credId
    )
    {
        return _credentialQueries.GetCredentialById(accountId, credId);
    }

    [HttpPost]
    [Route("{accountId}")]
    public ActionResult<Credential> AddCredential(
        [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
        CredentialModifyCommand credential
    )
    {
        var credId = _credentialRepository.CreateSingleCredential(credential);
        return CreatedAtAction(nameof(GetById), new { accountId = accountId, credId = credId }, credential);
    }

}
