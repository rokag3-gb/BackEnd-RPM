using Microsoft.AspNetCore.Mvc;
using RPM.Api.App;
using RPM.Domain.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace RPM.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class CredentialController : ControllerBase
{
  

    private readonly ILogger<CredentialController> _logger;
    private readonly ICredentialQueries _credentialQueries;

    public CredentialController(
        ILogger<CredentialController> logger,
        ICredentialQueries credentialQueries)
    {
        _logger = logger;
        _credentialQueries = credentialQueries;
    }

    // Api for querying list of Credentials
    [HttpGet]
    [Route("{accountId}")]
    public IEnumerable<Credential> GetList(
        [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
        [SwaggerParameter("클라우드 벤더 코드(VEN-XXX 형식)", Required = false)] string? vendor,
        [SwaggerParameter("자격증명 이름 검색 키워드", Required = false)] string? credName,
        [SwaggerParameter("자격증명 사용 여부", Required = false)] bool isEnabled
    )
    {
        return _credentialQueries.GetCredentials(accountId, vendor, credName, isEnabled);
    }

}
