using Microsoft.AspNetCore.Mvc;
using RPM.Infra.Data.Repositories;
using RPM.Api.App.Queries;
using RPM.Domain.Dto;
using RPM.Api.App.Commands;
using RPM.Domain.Models;
using RPM.Infra.Clients;
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
    private readonly IAMClient _iamClient;
    private readonly SalesClient _salesClient;
    private readonly IMapper _mapper;

    public CredentialController(
        ILogger<CredentialController> logger,
        ICredentialQueries credentialQueries,
        ICredentialRepository credentialRepository,
        IAMClient iamClient,
        SalesClient salesClient,
        IMapper mapper)
    {
        _logger = logger;
        _credentialQueries = credentialQueries;
        _credentialRepository = credentialRepository;
        _iamClient = iamClient;
        _salesClient = salesClient;
        _mapper = mapper;
    }

    // Api for querying list of Credentials
    [HttpGet]
    [Route("{accountId}/credentials")]
    public async Task<IEnumerable<CredentialDto>> GetList(
        [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
        [SwaggerParameter("클라우드 벤더 코드(VEN-XXX 형식)", Required = false)] string? vendor,
        [SwaggerParameter("자격증명 이름 검색 키워드", Required = false)] string? credName,
        [SwaggerParameter("자격증명 사용 여부", Required = false)] bool? isEnabled = null
    )
    {
        var credentials = _credentialQueries.GetCredentials(accountId, vendor, credName, isEnabled);
        var saverIdsSet = credentials.Select(x => x.SaverId).ToHashSet();
        var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        var userList = await _iamClient.ResolveUserList(token, saverIdsSet);
        var codeList = await _salesClient.GetKindCodeChilds(token, "000-VEN");
        if(userList == null)
        {
            userList = new List<UserListItem>();
        }
        if(codeList == null)
        {
            codeList = new List<Code>();
        }

        var result = from credential in credentials
                     join userRaw in userList on credential.SaverId equals userRaw.Id into joinedUsers
                     join codeRaw in codeList on credential.Vendor equals codeRaw.CodeKey into joinedCodes
                     from user in joinedUsers.DefaultIfEmpty()
                     from code in joinedCodes.DefaultIfEmpty()
                     select new CredentialDto
                     {
                         CredId = credential.CredId,
                         AccountId = credential.AccountId,
                         Vendor = credential.Vendor,
                         VendorName = code?.Name?? "",
                         CredName = credential.CredName,
                         IsEnabled = credential.IsEnabled,
                         CredData = credential.CredData,
                         Note = credential.Note,
                         SavedAt = credential.SavedAt,
                         SaverId = credential.SaverId,
                         SaverName = user?.Username?? ""
                     };

        return result;
    }

    [HttpGet]
    [Route("{accountId}/credential/{credId}")]
    public async Task<CredentialDto?> GetById(
        [SwaggerParameter("대상 조직 ID", Required = true)] long accountId,
        [SwaggerParameter("자격증명 ID", Required = false)] long credId
    )
    {
        var credential = _credentialQueries.GetCredentialById(accountId, credId);
        var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        var user = await _iamClient.ResolveUser(token, credential.SaverId);
        var vendor = await _salesClient.GetCodeByCodeKey(token, credential.Vendor);
        return new CredentialDto(){
            CredId = credential.CredId,
            AccountId = credential.AccountId,
            Vendor = credential.Vendor,
            VendorName = vendor?.Name?? "",
            CredName = credential.CredName,
            IsEnabled = credential.IsEnabled,
            CredData = credential.CredData,
            Note = credential.Note,
            SavedAt = credential.SavedAt,
            SaverId = user?.Id?? "",
            SaverName = user?.Username?? ""
        };
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
