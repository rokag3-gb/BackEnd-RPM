using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;

namespace RPM.Infra.Clients;

public class IAMClient
{
    private readonly HttpClient _httpClient;

    public IAMClient()
    {
        _httpClient = new HttpClient();
    }

    public IAMClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public virtual async Task<User?> ResolveUser(
        String authorization,
        String userId,
        String? alternativeBaseUrl = null
    )
    {
        string url = $"/users/{userId}";

        if (!string.IsNullOrEmpty(alternativeBaseUrl))
        {
            url = $"{alternativeBaseUrl}/users/{userId}";
        }

        using (var request = new HttpRequestMessage(HttpMethod.Get, url))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authorization);
            var response = await _httpClient.SendAsync(
                request,
                new CancellationTokenSource().Token
            );

            try
            {
                response.EnsureSuccessStatusCode();
                using var contentStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<User>(contentStream);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public virtual async Task<List<UserListItem>?> ResolveUserList(
        String authorization,
        HashSet<String> userIds,
        String? alternativeBaseUrl = null
    )
    {
        var queryParams = "";
        foreach (String userId in userIds)
        {
            queryParams += "ids=" + userId + "&";
        }

        string url = $"/iam/users?{queryParams}";

        if (!string.IsNullOrEmpty(alternativeBaseUrl))
        {
            url = $"{alternativeBaseUrl}/users?{queryParams}";
        }

        using (var request = new HttpRequestMessage(HttpMethod.Get, url))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authorization);
            var response = await _httpClient.SendAsync(
                request,
                new CancellationTokenSource().Token
            );

            try
            {
                response.EnsureSuccessStatusCode();
                using var contentStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<List<UserListItem>>(contentStream);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

public record User(
    [property: JsonPropertyName("id")] String Id,
    [property: JsonPropertyName("createdTimestamp")] long CreatedTimestamp,
    [property: JsonPropertyName("username")] String Username,
    [property: JsonPropertyName("enabled")] Boolean Enabled,
    [property: JsonPropertyName("firstName")] String FirstName,
    [property: JsonPropertyName("lastName")] String LastName,
    [property: JsonPropertyName("email")] String Email,
    [property: JsonPropertyName("requiredActions")] String[] RequiredActions,
    [property: JsonPropertyName("createDate")] String CreateDate,
    [property: JsonPropertyName("creator")] String Creator,
    [property: JsonPropertyName("modifyDate")] String ModifyDate,
    [property: JsonPropertyName("modifier")] String Modifier
);

public record UserListItem(
    [property: JsonPropertyName("id")] String Id,
    [property: JsonPropertyName("username")] String Username,
    [property: JsonPropertyName("enabled")] Boolean Enabled,
    [property: JsonPropertyName("firstName")] String FirstName,
    [property: JsonPropertyName("lastName")] String LastName,
    [property: JsonPropertyName("email")] String Email,
    [property: JsonPropertyName("groups")] String Groups,
    [property: JsonPropertyName("roles")] String Roles,
    [property: JsonPropertyName("OpenId")] String OpenId,
    [property: JsonPropertyName("createDate")] String CreateDate,
    [property: JsonPropertyName("creator")] String Creator,
    [property: JsonPropertyName("modifyDate")] String ModifyDate,
    [property: JsonPropertyName("modifier")] String Modifier
);

public class UserMapperProfile : Profile
{
    public UserMapperProfile()
    {
        CreateMap<UserListItem, User>()
            .ForCtorParam("CreatedTimestamp", opt => opt.MapFrom(src => 0))
            .ForCtorParam("RequiredActions", opt => opt.MapFrom(src => new String[] { }))
            .ForMember(dest => dest.CreatedTimestamp, opt => opt.NullSubstitute(0))
            .ForMember(dest => dest.RequiredActions, opt => opt.NullSubstitute(new String[] { }))
            .ForSourceMember(src => src.Groups, opt => opt.DoNotValidate())
            .ForSourceMember(src => src.Roles, opt => opt.DoNotValidate())
            .ForSourceMember(src => src.OpenId, opt => opt.DoNotValidate());
    }
}
