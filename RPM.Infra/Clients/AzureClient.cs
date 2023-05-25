using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Resources;

namespace RPM.Infra.Clients;

public class AzureClient
{
    public AzureClient(ClientSecretCredential credential)
    {
        _credential = credential;
    }

    private readonly ClientSecretCredential _credential;

    public async Task<AsyncPageable<VirtualMachineResource>> ListAzureVMs()
    {
        ArmClient armClient = new ArmClient(_credential);
        SubscriptionResource subscription = await armClient.GetDefaultSubscriptionAsync();
        var vmCollection = subscription.GetVirtualMachinesAsync();
        return vmCollection;
    }
}
