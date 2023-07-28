using RPM.Domain.Models;
using RPM.Api.App.Commands;
using RPM.Api.App.Mappers;
using RPM.Domain.Dto;
using RPM.Api.Controllers;
using RPM.Infra.Data.Repositories;
using RPM.Api.App.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Principal;
using System.Security.Claims;
using Moq;
using AutoMapper;
using RPM.Infra.Clients;

namespace RPM.Api.Tests.Controllers;

public class CredentialControllerTests
{
    private readonly IMapper _mapper;
    public CredentialControllerTests()
    {
        _mapper = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<CredentialMapperProfile>();
        }).CreateMapper();
    }

    [Fact]
    public async void GetById()
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
            Mock.Of<IAMClient>(),
            Mock.Of<SalesClient>(),
            Mock.Of<IMapper>()
        );

        var result = await controller.GetById(1, 1);
        Assert.IsType<Credential>(result);
        Assert.Equal("VEN-XXX", result.Vendor);
        Assert.Equal("test", result.CredName);
    }

    [Fact]
    public async void GetList()
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
            Mock.Of<IAMClient>(),
            Mock.Of<SalesClient>(),
            Mock.Of<IMapper>()
        );

        var result = await controller.GetList(1, "", "", true);
        Assert.Equal(true, typeof(IEnumerable<Credential>).IsAssignableFrom(result.GetType()));
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async void AddCredential()
    {
        var mockRepo = new Mock<ICredentialRepository>();
        var mockInput = new CredentialModifyCommand();
        var mockData = new Credential()
        {
            CredId = 1,
            AccountId = 1,
            Vendor = "VEN-XXX",
            CredName = "test",
            IsEnabled = true
        };

        mockRepo
            .Setup(x => x.CreateSingleCredential(It.IsAny<CredentialModifyDto>()))
            .Returns(mockData);

        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
                new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "123"),
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
            Mock.Of<IAMClient>(),
            Mock.Of<SalesClient>(),
            _mapper
        ){
            ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
                {
                    User = user
                }
            }
        };

        var result = controller.AddCredential(1, mockInput);
        Assert.IsType<ActionResult<Credential>>(result);
        mockRepo.Verify(r => r.CreateSingleCredential(It.IsAny<CredentialModifyDto>()));
    }

    [Fact]
    public void UpdateCredential()
    {
        var mockRepo = new Mock<ICredentialRepository>();
        var mockInput = new CredentialModifyCommand();
        var mockData = new Credential()
        {
            CredId = 1,
            AccountId = 1,
            Vendor = "VEN-XXX",
            CredName = "test",
            IsEnabled = true
        };

        mockRepo
            .Setup(x => x.UpdateSingleCredential(It.IsAny<long>(), It.IsAny<CredentialModifyDto>()))
            .Returns(mockData);

        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
                new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "123"),
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
            Mock.Of<IAMClient>(),
            Mock.Of<SalesClient>(),
            _mapper
        ){
            ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
                {
                    User = user
                }
            }
        };

        var result = controller.UpdateCredential(1, 1, mockInput);
        Assert.IsType<ActionResult<Credential>>(result);
        mockRepo.Verify(r => r.UpdateSingleCredential(It.IsAny<long>(), It.IsAny<CredentialModifyDto>()));
    }

    [Fact]
    public void DeleteById()
    {
        var mockRepo = new Mock<ICredentialRepository>();

        mockRepo.Setup(x => x.DeleteSingleCredential(It.IsAny<long>(), It.IsAny<long>()));

        var controller = new CredentialController(
            null,
            Mock.Of<ICredentialQueries>(),
            mockRepo.Object,
            Mock.Of<IAMClient>(),
            Mock.Of<SalesClient>(),
            _mapper
        );

        controller.DeleteById(1, 1);
        mockRepo.Verify(r => r.DeleteSingleCredential(It.IsAny<long>(), It.IsAny<long>()));
    }
}
