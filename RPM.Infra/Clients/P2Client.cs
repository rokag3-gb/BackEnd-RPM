using Grpc.Net.Client;
using P2.API.Services.Schedule;

namespace RPM.Infra.Clients;

public class P2Client {
    private GrpcChannel _grpcChannel;
    public P2Client(string serviceAddress) {
        _grpcChannel = GrpcChannel.ForAddress(serviceAddress);
    }

    public void HelloWorld() {
        var client = new ScheduleCreateApiService.ScheduleCreateApiServiceClient(_grpcChannel);
        var request = new CreateSchedulesRequest(){
            JobId = 1
        };
        request.CreateSchedules.Add(new CreateScheduleData(){
            AccountId = 1,
            ScheduleName = "test",
            Cron = "*/5 * * * * *",
            IsEnable = true,
            ActivateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            ExpireDate = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd HH:mm:ss"),
            Note = "test",
            SaveUserId = "sss",
        });
        client.CreateSchedules(request);
    }
}