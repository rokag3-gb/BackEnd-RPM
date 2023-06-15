using Google.Apis.Auth.OAuth2;
using Google.Cloud.Compute.V1;
using Google.Apis.Services;
using System.Threading.Channels;

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
        var builder = new InstancesClientBuilder() { GoogleCredential  = _credential };
        var projId = ((ServiceAccountCredential)_credential.UnderlyingCredential).ProjectId;
        var client = builder.Build();
        var response = client.AggregatedList(projId);
        var instanceList = response.Select(item => item.Value).ToList()
            .Select(i => i.Instances).ToList()
            .SelectMany(i => i).ToList();
        return instanceList;
    }
}
