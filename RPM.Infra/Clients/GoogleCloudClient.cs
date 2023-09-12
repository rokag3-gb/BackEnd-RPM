using Google.Apis.Auth.OAuth2;
using Google.Cloud.Compute.V1;
using Google.Apis.Services;
using System.Threading.Channels;
using RPM.Domain.Dto;
using Grpc.Core;

namespace RPM.Infra.Clients;

public class GoogleCloudClient
{
    private readonly GoogleCredential _credential;

    public GoogleCloudClient(string serviceAccountJson)
    {
        _credential = GoogleCredential.FromJson(serviceAccountJson);
    }

    public List<Instance> GetGcloudComputeEngines()
    {
        var builder = new InstancesClientBuilder() { GoogleCredential = _credential };
        var projId = ((ServiceAccountCredential)_credential.UnderlyingCredential).ProjectId;
        var client = builder.Build();
        var response = client.AggregatedList(projId);
        var instanceList = response
            .Select(item => item.Value)
            .ToList()
            .Select(i => i.Instances)
            .ToList()
            .SelectMany(i => i)
            .ToList();
        return instanceList;
    }

    public async Task<InstancesStatusDto> GetGcloudComputeEngineStatus(
        string regionCode,
        string instanceId
    )
    {
        var builder = new InstancesClientBuilder() { GoogleCredential = _credential };
        var projId = ((ServiceAccountCredential)_credential.UnderlyingCredential).ProjectId;
        var client = builder.Build();
        var vm = await client.GetAsync(projId, regionCode, instanceId);
        return new InstancesStatusDto()
        {
            Status = vm.Status == "RUNNING" ? "running" : "stopped",
            StatusCodeFromVendor = vm.Status
        };
    }

    public async Task<bool> ToggleGcloudComputeEnginePowerAsync(
        string regionCode,
        string instanceId,
        bool power
    )
    {
        var builder = new InstancesClientBuilder() { GoogleCredential = _credential };
        var projId = ((ServiceAccountCredential)_credential.UnderlyingCredential).ProjectId;
        var client = builder.Build();
        var vm = await client.GetAsync(projId, regionCode, instanceId);
        if (power)
        {
            StartInstanceRequest request = new StartInstanceRequest
            {
                Instance = vm.Name,
                Project = projId,
                Zone = regionCode
            };
            try
            {
                var response = client.Start(request);
            }
            catch (RpcException e)
            {
                Console.WriteLine($"Error starting instance: {e.Status.Detail}");
                Console.WriteLine(e.StackTrace);
                Console.WriteLine(e.Trailers);
                return false;
            }
        }
        else
        {
            StopInstanceRequest request = new StopInstanceRequest
            {
                Instance = vm.Name,
                Project = projId,
                Zone = regionCode
            };
            try
            {
                var response = client.Stop(request);
            }
            catch (RpcException e)
            {
                Console.WriteLine($"Error stopping instance: {e.Status.Detail}");
                Console.WriteLine(e.StackTrace);
                Console.WriteLine(e.Trailers);
                return false;
            }
        }
        return true;
    }
}
