using Grpc.Net.Client;
using P2.API.Services.Schedule;
using P2.API.Services.Job;

namespace RPM.Infra.Clients;

public interface IP2Client {
    Task<long> RegisterJobYaml(long accountId, string workflowYaml, string note, string savedByUserId);
    void CreateScheduleForJob(long accountId, long jobId, string scheduleName, string cronExpression, string note, string savedByUserId);
}

public class P2Client : IP2Client
{
    private GrpcChannel _grpcChannel;

    public P2Client(string serviceAddress)
    {
        _grpcChannel = GrpcChannel.ForAddress(serviceAddress);
    }

    public async Task<long> RegisterJobYaml(long accountId, string workflowYaml, string note, string savedByUserId)
    {
        var jobClient = new JobCreateApiService.JobCreateApiServiceClient(_grpcChannel);
        var jobCreateReq = new JobCreateRequest()
        {
            AccountId = accountId,
            AppCode = "APP_RPM",
            JobName = "RPM VM Power Switch DAG",
            WorkflowContentYaml = workflowYaml,
            Note = note,
            IsEnable = true,
            SaveUserId = savedByUserId,
        };
        var res = await jobClient.CreateAsync(jobCreateReq);
        return res.JobId;
    }

    public void CreateScheduleForJob(long accountId, long jobId, string scheduleName, string cronExpression, string note, string savedByUserId)
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
                ActivateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ExpireDate = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd HH:mm:ss"),
                Note = note,
                SaveUserId = savedByUserId,
            }
        );
        var response = client.CreateSchedules(request);
    }
}
