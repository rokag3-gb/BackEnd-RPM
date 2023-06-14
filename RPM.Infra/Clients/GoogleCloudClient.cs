using Google.Apis.Auth.OAuth2;
using Google.Cloud.Compute.V1;
using Google.Apis.Services;
using System.Threading.Channels;

namespace RPM.Infra.Clients;

public class GoogleCloudClient
{
    private readonly GoogleCredential _credential;

    public GoogleCloudClient()
    {
        _credential = GoogleCredential.GetApplicationDefault();
    }

    public List<Instance> GetGcloudComputeEngines()
    {
        var builder = new InstancesClientBuilder() { Credential = _credential };
        var client = builder.Build();

        var request = new ListInstancesRequest { };
        var response = client.List(request);
        return response.ToList();
    }
}
