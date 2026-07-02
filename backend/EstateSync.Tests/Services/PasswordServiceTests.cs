using FluentAssertions;
using NUnit.Framework;
using EstateSync.Api.Services.Auth;

namespace EstateSync.Tests.Services;

[TestFixture]
public class PasswordServiceTests
{
    private PasswordService _service;

    [SetUp]
    public void Setup()
    {
        _service = new PasswordService();
    }

    [Test]
    public void HashPassword_ShouldReturnHashedString()
    {
        var password = "SecurePassword123!";
        
        var hash = _service.HashPassword(password);

        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe(password);
        hash.Should().StartWith("$2"); // BCrypt prefix
    }

    [Test]
    public void VerifyPassword_ShouldReturnTrueForCorrectPassword()
    {
        var password = "SecurePassword123!";
        var hash = _service.HashPassword(password);

        var result = _service.VerifyPassword(password, hash);

        result.Should().BeTrue();
    }

    [Test]
    public void VerifyPassword_ShouldReturnFalseForIncorrectPassword()
    {
        var password = "SecurePassword123!";
        var wrongPassword = "WrongPassword123!";
        var hash = _service.HashPassword(password);

        var result = _service.VerifyPassword(wrongPassword, hash);

        result.Should().BeFalse();
    }
}
