using MediatR;
using RPM.Api.App.Queries;
using RPM.Infra.Clients;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RPM.Api.App.Commands;

public class RegisterInstanceJobCommandHandler : IRequestHandler<RegisterInstanceJobCommand, int>
{
    ICredentialQueries _credentialQueries;
    IInstanceQueries _instanceQueries;
    IP2Client _p2Client;
    private readonly IConfiguration _config;

    public RegisterInstanceJobCommandHandler(
        ICredentialQueries credentialQueries,
        IInstanceQueries instanceQueries,
        IP2Client p2Client,
        IConfiguration config
    )
    {
        _credentialQueries = credentialQueries;
        _instanceQueries = instanceQueries;
        _p2Client = p2Client;
        _config = config;
    }

    public async Task<int> Handle(
        RegisterInstanceJobCommand request,
        CancellationToken cancellationToken
    )
    {
        // VM, Credential 목록 쿼리
        var instanceList = _instanceQueries.GetInstancesByIds(
            request.AccountId,
            request.InstanceIds
        );
        var credentialIds = instanceList.GroupBy(x => x.CredId).Select(x => x.First().CredId);
        var credentialList = _credentialQueries.GetCredentialsByIds(
            request.AccountId,
            credentialIds
        );
        // VM 목록, Credential Dict 만들기
        var instanceListStripped = instanceList
            .Select(
                x =>
                    new
                    {
                        ResourceId = x.ResourceId,
                        Name = x.Name,
                        Region = x.Region,
                        Vendor = x.Vendor,
                        CredId = x.CredId,
                        Info = x.Info
                    }
            )
            .ToList();
        var credentialDict = credentialList.ToDictionary(
            x => x.CredId,
            x => new { CredData = x.CredData }
        );

        var yamlWorkflowFilePath = _config.GetConnectionString("YamlWorkflowFilePath");
        if (string.IsNullOrEmpty(yamlWorkflowFilePath))
        {
            return -1;
        }
        try
        {
            // Open the file using File.Open
            using (FileStream fileStream = File.Open(yamlWorkflowFilePath, FileMode.Open))
            {
                // Perform operations on the opened file
                // For example, you can read the file content using a StreamReader
                using (StreamReader streamReader = new StreamReader(fileStream))
                {
                    string fileContent = streamReader.ReadToEnd();

                    //YAML 파싱
                    var deserializer = new DeserializerBuilder()
                        .WithNamingConvention(UnderscoredNamingConvention.Instance)  // see height_in_inches in sample yml 
                        .Build();
                        // var p = deserializer.Deserialize(fileContent);

                    // YAML 로딩하여 VM 목록, Credential Dict 데이터 삽입
                    var newJobId = await _p2Client.RegisterJobYaml(
                        request.AccountId,
                        fileContent,
                        request.Note,
                        request.SavedByUserId
                    );
                    return 0;
                }
            }
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("File not found.");
        }
        catch (IOException)
        {
            Console.WriteLine("An error occurred while opening the file.");
        }
        return 0;
    }
}
