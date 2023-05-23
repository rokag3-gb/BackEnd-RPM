using RPM.Domain.Models;
using RPM.Domain.Commands;
using RPM.Domain.Mappers;
using RPM.Domain.Dto;
using RPM.Api.Controllers;
using RPM.Api.App.Repository;
using RPM.Api.App.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Principal;
using System.Security.Claims;
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
        Assert.Equal(true, typeof(IEnumerable<Credential>).IsAssignableFrom(result.GetType()));
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void AddCredential()
    {
        var mockRepo = new Mock<ICredentialRepository>();
        var mockInput = new CredentialModifyDto();
        var mockData = new Credential();

        mockRepo
            .Setup(x => x.CreateSingleCredential(It.IsAny<CredentialModifyCommand>()))
            .Returns(mockData)
            .Verifiable();

        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
                new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "SomeValueHere"),
                    new Claim(ClaimTypes.Name, "a@b.c")
                    // other required and custom claims
                },
                "TestAuthentication"
            )
        );

        var controller = new CredentialController(
            null,
            Mock.Of<ICredentialQueries>(),
            mockRepo.Object,
            Mock.Of<IMapper>()
        );
        controller.ControllerContext = new ControllerContext();
        controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };
        
        var result = controller.AddCredential(1, mockInput);
        Assert.IsType<Credential>(result);
        mockRepo.Verify();
    }
}
