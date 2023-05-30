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

    public async Task<List<VirtualMachineResource>> ListAzureVMs()
    {
        ArmClient armClient = new ArmClient(_credential);
        SubscriptionResource subscription = await armClient.GetDefaultSubscriptionAsync();
        var rgCollection = subscription.GetResourceGroups().GetAllAsync();
        var vmList = new List<VirtualMachineResource>();
        await foreach (var rg in rgCollection)
        {
            var vmCollection = rg.GetVirtualMachines().GetAllAsync();
            await foreach (var vm in vmCollection)
            {
                vmList.Add(vm);
            }
            
        }
        // var vmCollection = subscription.GetVirtualMachinesAsync();
        return vmList;
    }
}
