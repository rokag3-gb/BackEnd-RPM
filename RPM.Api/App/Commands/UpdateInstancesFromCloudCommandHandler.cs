using MediatR;
using RPM.Api.App.Queries;
using RPM.Infra.Data.Repositories;
using RPM.Infra.Clients;
using RPM.Domain.Models;
using RPM.Domain.Dto;
using Azure.Identity;
using System.Text.Json;
using Azure.ResourceManager.Compute;
using System.Linq;
using AutoMapper;

namespace RPM.Api.App.Commands;

public class UpdateInstancesFromCloudCommandHandler
    : IRequestHandler<UpdateInstancesFromCloudCommand, int>
{
    ICredentialQueries _credentialQueries;
    IInstanceQueries _instanceQueries;
    IInstanceRepository _instanceRepository;
    IMapper _mapper;

    public UpdateInstancesFromCloudCommandHandler(
        ICredentialQueries credentialQueries,
        IInstanceQueries instanceQueries,
        IInstanceRepository instanceRepository,
        IMapper mapper
    )
    {
        _credentialQueries = credentialQueries;
        _instanceQueries = instanceQueries;
        _instanceRepository = instanceRepository;
        _mapper = mapper;
    }

    public async Task<int> Handle(
        UpdateInstancesFromCloudCommand request,
        CancellationToken cancellationToken
    )
    {
        var credential = _credentialQueries.GetCredentialById(request.AccountId, request.CredId);
        if(credential == null){
            return -1;
        }
        IEnumerable<Instance> fetchedInstanceList = new List<Instance>();
        var credData = JsonSerializer.Deserialize<JsonElement>(credential.CredData);
        switch (credential.Vendor)
        {
            case "VEN-AZP":
                fetchedInstanceList = await GetVMListFromAzure(
                    request.AccountId,
                    request.CredId,
                    credData.GetProperty("tenant_id").GetString(),
                    credData.GetProperty("client_id").GetString(),
                    credData.GetProperty("client_secret").GetString()
                );
                break;
        }

        if (fetchedInstanceList.Count() == 0)
        {
            return 0;
        }
        var fetchedInstanceResourceIds = fetchedInstanceList.Select(x => x.ResourceId).ToList();
        var currentInstances = _instanceQueries.GetInstances(request.AccountId, request.CredId);
        var currentInstanceResourceIds = currentInstances.Select(x => x.ResourceId).ToList();
        var instancesToDelete = currentInstances
            .Where(x => !fetchedInstanceResourceIds.Contains(x.ResourceId))
            .ToList();
        var instancesToUpdate = currentInstances
            .Where(x => fetchedInstanceResourceIds.Contains(x.ResourceId))
            .ToList();
        var instancesToInsert = fetchedInstanceList
            .Where(x => !currentInstanceResourceIds.Contains(x.ResourceId))
            .ToList();

        // Insert new instances
        var toInsertMappedList = _mapper.Map<List<Instance>, IEnumerable<InstanceModifyDto>>(
            instancesToInsert
        );
        var conn = _instanceRepository.GetConnection();
        using (var tx = conn.BeginTransaction())
        {
            var inserted = _instanceRepository.CreateMultipleInstances(toInsertMappedList, conn, tx);

            // Update existing instances
            var updated = _instanceRepository.UpdateMultipleInstances(instancesToUpdate, conn, tx);

            // Delete instances
            var deleted = _instanceRepository.DeleteMultipleInstances(
                instancesToDelete.Select(x => x.InstId).ToList(),
                conn, tx
            );
            tx.Commit();
            var affectedRows = inserted.Count() + updated.Count() + deleted;
            return affectedRows;
        }
    }

    private async Task<IEnumerable<Instance>> GetVMListFromAzure(
        long accountId,
        long credId,
        string tenantId,
        string clientId,
        string clientSecret
    )
    {
        var azureClient = new AzureClient(
            new ClientSecretCredential(tenantId, clientId, clientSecret)
        );
        var vmList = await azureClient.ListAzureVMs();
        var instanceList = new List<Instance>();
        await foreach (VirtualMachineResource vm in vmList)
        {
            instanceList.Add(
                new Instance()
                {
                    AccountId = accountId,
                    CredId = credId,
                    Vendor = "VEN-AZP",
                    ResourceId = vm.Data.Id,
                    Name = vm.Data.Name,
                    Region = vm.Data.Location,
                    Type = vm.Data.ResourceType,
                    Tags = JsonSerializer.Serialize(vm.Data.Tags),
                    Info = "",
                    Note = "",
                }
            );
        }
        return instanceList;
    }
}
