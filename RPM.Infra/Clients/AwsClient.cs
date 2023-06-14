using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Runtime;
using System.Linq;

namespace RPM.Infra.Clients;

public class AWSClient
{
    private readonly BasicAWSCredentials _credential;

    public AWSClient(BasicAWSCredentials credential)
    {
        _credential = credential;
    }

    public async Task<List<Instance>> ListAwsVMsAsync(RegionEndpoint region)
    {
        var clientConfig = new AmazonEC2Config
        {
            RegionEndpoint = region // Specify the region for the client
        };
        var ec2Client = new AmazonEC2Client(_credential, clientConfig);

        // Use the EC2 client to list instances
        var request = new DescribeInstancesRequest();
        var response = await ec2Client.DescribeInstancesAsync(request);
        var ec2List = response.Reservations.Select(i => i.Instances)
            .ToList().SelectMany(i => i).ToList();
        return ec2List;
    }
}
