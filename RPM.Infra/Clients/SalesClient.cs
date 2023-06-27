using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;

namespace RPM.Infra.Clients;

public class SalesClient
{
    private readonly HttpClient _httpClient;

    public SalesClient()
    {
        _httpClient = new HttpClient();
    }

    public SalesClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public virtual async Task<Code?> GetCodeByCodeKey(
        String authorization,
        String codeKey,
        String? alternativeBaseUrl = null
    )
    {
        string url = $"/sales/code/{codeKey}";

        if (!string.IsNullOrEmpty(alternativeBaseUrl))
        {
            url = $"{alternativeBaseUrl}/sales/code/{codeKey}";
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
                return await JsonSerializer.DeserializeAsync<Code>(contentStream);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public virtual async Task<List<Code>?> GetKindCodeChilds(
        String authorization,
        String kindCode,
        String? alternativeBaseUrl = null
    )
    {

        string url = $"/sales/code/{kindCode}/childs";

        if (!string.IsNullOrEmpty(alternativeBaseUrl))
        {
            url = $"{alternativeBaseUrl}/sales/code/{kindCode}/childs";
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
                return await JsonSerializer.DeserializeAsync<List<Code>>(contentStream);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

public record Code(
    [property: JsonPropertyName("codeNo")] int CodeNo,
    [property: JsonPropertyName("kindCode")] String KindCode,
    [property: JsonPropertyName("subCode")] String SubCode,
    [property: JsonPropertyName("codeKey")] String CodeKey,
    [property: JsonPropertyName("name")] String Name,
    [property: JsonPropertyName("sort")] int Sort,
    [property: JsonPropertyName("isUse")] bool IsUse,
    [property: JsonPropertyName("remark")] String Remark,
    [property: JsonPropertyName("value1")] String Value1,
    [property: JsonPropertyName("value2")] String Value2,
    [property: JsonPropertyName("value3")] String Value3,
    [property: JsonPropertyName("saveDate")] String SaveDate,
    [property: JsonPropertyName("saveId")] String SaveId
);
