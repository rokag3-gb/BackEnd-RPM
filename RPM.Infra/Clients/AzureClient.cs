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

    public async Task<string> GetAzureVMStatus(string rgName, string vmName)
    {
        // Create an instance of the ComputeManagementClient using your Azure credentials
        ArmClient armClient = new ArmClient(_credential);

        // Get the VM by resource group name and VM name
        SubscriptionResource subscription = await armClient.GetDefaultSubscriptionAsync();
        // first we need to get the resource group
        ResourceGroupResource resourceGroup = await subscription
            .GetResourceGroups()
            .GetAsync(rgName);
        // Now we get the virtual machine collection from the resource group
        VirtualMachineCollection vmCollection = resourceGroup.GetVirtualMachines();
        VirtualMachineResource vm = await vmCollection.GetAsync(vmName);
        var vmPowerStatues = vm.InstanceView().Value.Statuses;
        var vmState = vmPowerStatues.Any(
            s => s.Code == "PowerState/running" || s.Code == "PowerState/starting"
        );
        return vmState ? "running" : "stopped";
        // ... print other properties you want to retrieve
    }
}
