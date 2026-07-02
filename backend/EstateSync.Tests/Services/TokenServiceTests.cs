using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using EstateSync.Api.Models;
using EstateSync.Api.Services.Tokens;

namespace EstateSync.Tests.Services;

[TestFixture]
public class TokenServiceTests
{
    private Mock<IConfiguration> _configMock;
    private TokenService _service;

    [SetUp]
    public void Setup()
    {
        _configMock = new Mock<IConfiguration>();
        
        var configSectionMock = new Mock<IConfigurationSection>();
        _configMock.Setup(x => x["Jwt:Issuer"]).Returns("TestIssuer");
        _configMock.Setup(x => x["Jwt:Audience"]).Returns("TestAudience");
        _configMock.Setup(x => x["Jwt:ExpiryMinutes"]).Returns("60");
        _configMock.Setup(x => x["Jwt:SecretKey"]).Returns("ThisIsAVerySecureKeyThatIsAtLeast32BytesLong!");

        _service = new TokenService(_configMock.Object);
    }

    [Test]
    public void GenerateAccessToken_ShouldReturnValidJwtToken()
    {
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            UserRoles = new List<UserRole>
            {
                new UserRole { Role = new Role { Name = "Tenant" } }
            }
        };

        var token = _service.GenerateAccessToken(user);

        token.Should().NotBeNullOrEmpty();
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();
    }

    [Test]
    public void GenerateRefreshToken_ShouldReturnBase64String()
    {
        var token = _service.GenerateRefreshToken();

        token.Should().NotBeNullOrEmpty();
        Action act = () => Convert.FromBase64String(token);
        act.Should().NotThrow();
    }

    [Test]
    public void GetPrincipalFromToken_ShouldReturnNull_ForInvalidToken()
    {
        var result = _service.GetPrincipalFromToken("invalid.jwt.token");
        result.Should().BeNull();
    }

    [Test]
    public void GetPrincipalFromToken_ShouldReturnPrincipal_ForValidToken()
    {
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            UserRoles = new List<UserRole>()
        };

        var token = _service.GenerateAccessToken(user);
        var principal = _service.GetPrincipalFromToken(token);

        principal.Should().NotBeNull();
        principal!.FindFirst(ClaimTypes.Email)?.Value.Should().Be(user.Email);
    }
}
