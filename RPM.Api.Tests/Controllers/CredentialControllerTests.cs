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
        var mockData = new Credential()
        {
            CredId = 1,
            AccountId = 1,
            Vendor = "VEN-XXX",
            CredName = "test",
            IsEnabled = true
        };

        mockRepo
            .Setup(x => x.CreateSingleCredential(It.IsAny<CredentialModifyCommand>()))
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
        mockRepo.Verify(r => r.CreateSingleCredential(It.IsAny<CredentialModifyCommand>()));
    }

    [Fact]
    public void UpdateCredential()
    {
        var mockRepo = new Mock<ICredentialRepository>();
        var mockInput = new CredentialModifyDto();
        var mockData = new Credential()
        {
            CredId = 1,
            AccountId = 1,
            Vendor = "VEN-XXX",
            CredName = "test",
            IsEnabled = true
        };

        mockRepo
            .Setup(x => x.UpdateSingleCredential(It.IsAny<long>(), It.IsAny<CredentialModifyCommand>()))
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
        mockRepo.Verify(r => r.UpdateSingleCredential(It.IsAny<long>(), It.IsAny<CredentialModifyCommand>()));
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
            _mapper
        );

        controller.DeleteById(1, 1);
        mockRepo.Verify(r => r.DeleteSingleCredential(It.IsAny<long>(), It.IsAny<long>()));
    }
}
