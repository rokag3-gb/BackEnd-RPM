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
    public IEnumerable<Credential> Get(
        [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
        [SwaggerParameter("클라우드 벤더 코드(VEN-XXX 형식)", Required = false)] string? vendor
    )
    {
        return _credentialQueries.GetCredentials(accountId, vendor);
    }

    // [HttpGet(Name = "GetWeatherForecast")]
    // public IEnumerable<WeatherForecast> Get()
    // {
    //     return Enumerable.Range(1, 5).Select(index => new WeatherForecast
    //     {
    //         Date = DateTime.Now.AddDays(index),
    //         TemperatureC = Random.Shared.Next(-20, 55),
    //         Summary = Summaries[Random.Shared.Next(Summaries.Length)]
    //     })
    //     .ToArray();
    // }
}
