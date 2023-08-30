using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Runtime;
using RPM.Domain.Dto;
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

    public async Task<InstancesStatusDto> GetAwsVMStatus(string regionCode, string instanceId)
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
        return new InstancesStatusDto() { Status = status, StatusCodeFromVendor = status };
    }

    public async Task<bool> ToggleAwsVMPowerAsync(string regionCode, string instanceId, bool power)
    {
        var clientConfig = new AmazonEC2Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(regionCode), // Specify the region for the client
        };
        var ec2Client = new AmazonEC2Client(_credential, clientConfig);

        if (power)
        {
            StartInstancesRequest request = new StartInstancesRequest
            {
                InstanceIds = new List<string> { instanceId }
            };
            StartInstancesResponse response = await ec2Client.StartInstancesAsync(request);
            return response.StartingInstances.Count > 0;
        }
        else
        {
            StopInstancesRequest request = new StopInstancesRequest
            {
                InstanceIds = new List<string> { instanceId }
            };
            StopInstancesResponse response = await ec2Client.StopInstancesAsync(request);
            return response.StoppingInstances.Count > 0;
        }
    }
}
