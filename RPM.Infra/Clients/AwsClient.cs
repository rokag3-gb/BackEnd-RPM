using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Runtime;
using System.Linq;

namespace RPM.Infra.Clients;

public class AWSClient
{
    private readonly BasicAWSCredentials _credential;

    public AWSClient(string accessKeyId, string accessKeySecret)
    {
        _credential = new BasicAWSCredentials(accessKeyId, accessKeySecret);
    }

    public async Task<List<Instance>> ListAwsVMsAsync(string regionCode)
    {
        var clientConfig = new AmazonEC2Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(regionCode), // Specify the region for the client
        };
        var ec2Client = new AmazonEC2Client(_credential, clientConfig);

        // Use the EC2 client to list instances
        var request = new DescribeInstancesRequest();
        var response = await ec2Client.DescribeInstancesAsync(request);
        var ec2List = response.Reservations
            .Select(i => i.Instances)
            .ToList()
            .SelectMany(i => i)
            .ToList();
        return ec2List;
    }

    public async Task<string> GetAwsVMStatus(string regionCode, string instanceId)
    {
        var clientConfig = new AmazonEC2Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(regionCode), // Specify the region for the client
        };
        var ec2Client = new AmazonEC2Client(_credential, clientConfig);
        // Create a request to describe EC2 instances
        DescribeInstancesRequest request = new DescribeInstancesRequest
        {
            InstanceIds = new List<string> { instanceId }
        };

        // Get the response with the list of instances
        DescribeInstancesResponse response = await ec2Client.DescribeInstancesAsync(request);

        // Iterate through the reservations and instances
        var status = response.Reservations.First().Instances.First().State.Name;
        return status;
    }
}
