using Grpc.Net.Client;
using P2.API.Services.Schedule;

namespace RPM.Infra.Clients;

public class P2Client {
    private GrpcChannel _grpcChannel;
    public P2Client(string serviceAddress) {
        _grpcChannel = GrpcChannel.ForAddress(serviceAddress);
    }

    public void HelloWorld() {
        var client = new  ScheduleCreateApiService.ScheduleCreateApiServiceClient(_grpcChannel);
        client.CreateSchedules(new CreateSchedulesRequest(){
            JobId = 1
            
        });
    }
}