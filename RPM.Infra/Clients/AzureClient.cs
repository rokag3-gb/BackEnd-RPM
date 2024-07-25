using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Resources;
using Microsoft.Identity.Client;
using RPM.Domain.Dto;

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

    public async Task<InstancesStatusDto> GetAzureVMStatus(string rgName, string vmName)
    {
       
        SubscriptionResource subscription;
        try
        {
             // Create an instance of the ComputeManagementClient using your Azure credentials
            ArmClient armClient = new ArmClient(_credential);
            // Get the VM by resource group name and VM name
            subscription = await armClient.GetDefaultSubscriptionAsync();
        }
        catch (MsalServiceException)
        {
            return new InstancesStatusDto()
            {
                Status = "credential-invalid",
                StatusCodeFromVendor = ""
            };
        }

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
        return new InstancesStatusDto()
        {
            Status = vmState ? "running" : "stopped",
            StatusCodeFromVendor = vmPowerStatues[1].Code,
        };
        // ... print other properties you want to retrieve
    }

    public async Task<bool> ToggleAzureVMPowerAsync(string rgName, string vmName, bool power)
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
        if (power)
        {
            var result = await vm.PowerOnAsync(0);
            return result.HasCompleted;
        }
        else
        {
            var result = await vm.PowerOffAsync(0);
            return result.HasCompleted;
        }
    }
}
