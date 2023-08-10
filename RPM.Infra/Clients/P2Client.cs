using Grpc.Net.Client;
using P2.API.Services.Schedule;
using P2.API.Services.Job;
using P2.API.Services.Commons;
using System.Collections;
using P2.API.Services.Run;
using Google.Cloud.Compute.V1;
using Grpc.Core;
using YamlDotNet.Core.Tokens;
using Amazon.EC2.Model;

namespace RPM.Infra.Clients;

public interface IP2Client
{
    Task<long> RegisterJobYaml(
        long accountId,
        string jobName,
        string workflowYaml,
        string note,
        string savedByUserId
    );
    void CreateScheduleForJob(
        long accountId,
        long jobId,
        string scheduleName,
        string cronExpression,
        DateTime activateDate,
        DateTime expireDate,
        string note,
        string savedByUserId
    );

    IEnumerable<JobScheduleData> GetSchedules(long jobId);
    Task<IEnumerable<RunData>> GetRuns(IEnumerable<long> jobIds, DateTime from, DateTime to, IEnumerable<RunState> runStates, string token);
    Task<RunData?> GetLatest(IEnumerable<long> jobIds, long? accountId, DateTime? from, DateTime? to, string? runState, string token);
}

public class P2Client : IP2Client
{
    private GrpcChannel _grpcChannel;

    public P2Client(string serviceAddress)
    {
        _grpcChannel = GrpcChannel.ForAddress(serviceAddress);
    }

    public async Task<long> RegisterJobYaml(
        long accountId,
        string jobName,
        string workflowYaml,
        string note,
        string savedByUserId
    )
    {
        var jobClient = new JobCreateApiService.JobCreateApiServiceClient(_grpcChannel);
        var jobCreateReq = new JobCreateRequest()
        {
            AccountId = accountId,
            AppCode = "APP-RPM",
            JobName = jobName,
            WorkflowContentYaml = workflowYaml,
            Note = note,
            IsEnable = true,
            SaveUserId = savedByUserId,
        };
        var res = await jobClient.CreateAsync(jobCreateReq);
        return res.JobId;
    }

    public void CreateScheduleForJob(
        long accountId,
        long jobId,
        string scheduleName,
        string cronExpression,
        DateTime activateDate,
        DateTime expireDate,
        string note,
        string savedByUserId
    )
    {
        var client = new ScheduleCreateApiService.ScheduleCreateApiServiceClient(_grpcChannel);
        var request = new CreateSchedulesRequest() { JobId = jobId };
        request.CreateSchedules.Add(
            new CreateScheduleData()
            {
                AccountId = accountId,
                ScheduleName = scheduleName,
                Cron = cronExpression,
                IsEnable = true,
                ActivateDate = activateDate.ToString("o"),
                ExpireDate = expireDate.ToString("o"),
                Note = note,
                SaveUserId = savedByUserId,
            }
        );
        var response = client.CreateSchedules(request);
    }

    public IEnumerable<JobScheduleData> GetSchedules(long jobId)
    {
        var client = new ScheduleGetApiService.ScheduleGetApiServiceClient(_grpcChannel);
        var request = new ScheduleGetRequest() { JobId = jobId };
        var response = client.GetSchedulesByJob(request);
        var list = response.Schedules.ToList();
        return list;
    }

    /// <summary>
    /// P2의 Run 데이터를 조회합니다
    /// </summary>
    public async Task<IEnumerable<RunData>> GetRuns(IEnumerable<long> jobIds, DateTime from, DateTime to, IEnumerable<RunState> runStates, string token)
    {
        var client = new RunGetApiService.RunGetApiServiceClient(_grpcChannel);

        var headers = new Grpc.Core.Metadata();
        headers.Add("Authorization", $"Bearer {token}");

        var request = new RunGetByJobRequest();
        request.PeriodFrom = from.ToString("o");
        request.PeriodTo = to.ToString("o");
        request.JobIds.AddRange(jobIds);
        request.RunState.AddRange(runStates);
        var response = await client.GetListByJobAsync(request, headers);
        return response.Runs;
    }

    public async Task<RunData?> GetLatest(IEnumerable<long> jobIds, long? accountId, DateTime? from, DateTime? to, string? runState, string token)
    {
        var client = new RunGetApiService.RunGetApiServiceClient(_grpcChannel);
        
        var headers = new Grpc.Core.Metadata();
        headers.Add("Authorization", $"Bearer {token}");

        var request = new RunGetLatestRequest();

        request.JobIds.AddRange(jobIds);
        if (accountId != null)
            request.AccountId = accountId.Value;
        request.From = from?.ToString("o");
        request.To = to?.ToString("o");
        request.RunState = runState;

        var response = await client.GetLatestAsync(request);

        return response?.Run;
    }
}
