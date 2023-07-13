using MediatR;
using RPM.Api.App.Queries;
using RPM.Infra.Clients;

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
        var instanceaList = _instanceQueries.GetInstancesByIds(
            request.AccountId,
            request.InstanceIds
        );
        var credentialIds = instanceaList.GroupBy(x => x.CredId).Select(x => x.First().CredId);
        var credentialList = _credentialQueries.GetCredentialsByIds(
            request.AccountId,
            credentialIds
        );
        // VM 목록, Credential Dict 만들기
        // YAML 로딩하여 VM 목록, Credential Dict 데이터 삽입
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
