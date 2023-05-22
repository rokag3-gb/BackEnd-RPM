using RPM.Domain.Models;
using RPM.Domain.Mappers;
using RPM.Api.Controllers;
using RPM.Api.App.Repository;
using RPM.Api.App.Queries;
using Moq;
using AutoMapper;

namespace RPM.Api.Tests.Controllers;

public class CredentialControlerTests
{
    [Fact]
    public void GetById()
    {
        var mockQueries = new Mock<ICredentialQueries>();
        var mockData = new Credential()
        {
            CredId = 1,
            AccountId = 1,
            Vendor = "VEN-XXX",
            CredName = "test",
            IsEnabled = true
        };
        mockQueries
            .Setup(x => x.GetCredentialById(It.IsAny<long>(), It.IsAny<long>()))
            .Returns(mockData);

        var controller = new CredentialController(
            null,
            mockQueries.Object,
            Mock.Of<ICredentialRepository>(),
            Mock.Of<IMapper>()
        );

        var result = controller.GetById(1, 1);
        Assert.IsType<Credential>(result);
        Assert.Equal("VEN-XXX", result.Vendor);
        Assert.Equal("test", result.CredName);
    }

    [Fact]
    public void GetList()
    {
        var mockQueries = new Mock<ICredentialQueries>();
        var mockData = new List<Credential>()
        {
            new Credential()
            {
                CredId = 1,
                AccountId = 1,
                Vendor = "VEN-XXX",
                CredName = "test",
                IsEnabled = true
            },
            new Credential()
            {
                CredId = 2,
                AccountId = 1,
                Vendor = "VEN-ABC",
                CredName = "ABC",
                IsEnabled = false
            }
        };
        mockQueries
            .Setup(
                x =>
                    x.GetCredentials(
                        It.IsAny<long>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<bool>()
                    )
            )
            .Returns(mockData);

        var controller = new CredentialController(
            null,
            mockQueries.Object,
            Mock.Of<ICredentialRepository>(),
            Mock.Of<IMapper>()
        );

        var result = controller.GetList(1, "", "", true);
        Assert.IsType<List<Credential>>(result);
        Assert.Equal(2, result.Count());
    }
}
