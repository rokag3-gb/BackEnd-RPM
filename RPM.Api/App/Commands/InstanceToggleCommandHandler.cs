using MediatR;
using RPM.Api.App.Queries;
using RPM.Infra.Clients;
using System.Text.Json;
using Azure.Identity;

namespace RPM.Api.App.Commands;

public class InstanceToggleCommandHandler
    : IRequestHandler<InstanceToggleCommand, bool>
{
    ICredentialQueries _credentialQueries;
    IInstanceQueries _instanceQueries;
    private readonly IConfiguration _config;

    public InstanceToggleCommandHandler(
        ICredentialQueries credentialQueries,
        IInstanceQueries instanceQueries,
        IConfiguration config
    )
    {
        _credentialQueries = credentialQueries;
        _instanceQueries = instanceQueries;
        _config = config;
    }

    public async Task<bool> Handle(
        InstanceToggleCommand request,
        CancellationToken cancellationToken
    )
    {
        // VM, Credential 목록 쿼리
        var instance = _instanceQueries.GetInstanceById(request.AccountId, request.InstanceId);
        var credential = _credentialQueries.GetCredentialById(request.AccountId, instance.CredId);

        bool result = false;
        var credData = JsonSerializer.Deserialize<JsonElement>(credential.CredData);
        switch (credential.Vendor)
        {
            case "VEN-AZT":
                var azureClient = new AzureClient(
                    new ClientSecretCredential(
                        credData.GetProperty("tenant_id").GetString(),
                        credData.GetProperty("client_id").GetString(),
                        credData.GetProperty("client_secret").GetString()
                    )
                );
                var instanceInfo = JsonSerializer.Deserialize<JsonElement>(instance.Info);
                result = await azureClient.ToggleAzureVMPowerAsync(
                    instanceInfo.GetProperty("Id").GetProperty("ResourceGroupName").GetString(),
                    instance.Name,
                    request.PowerToggle
                );
                break;
            case "VEN-AWS":
                var awsClient = new AWSClient(
                    credData.GetProperty("access_key_id").GetString(),
                    credData.GetProperty("access_key_secret").GetString()
                );
                result = await awsClient.ToggleAwsVMPowerAsync(
                    credData.GetProperty("region_code").GetString(),
                    instance.ResourceId,
                    request.PowerToggle
                );
                break;
            case "VEN-GCP":
                var gcloudClient = new GoogleCloudClient(credential.CredData);
                result = await gcloudClient.ToggleGcloudComputeEnginePowerAsync(
                    instance.Region,
                    instance.ResourceId,
                    request.PowerToggle
                );
                break;
        }

        return result;
    }
}
