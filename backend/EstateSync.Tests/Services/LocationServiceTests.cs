using EstateSync.Api.Exceptions;
using EstateSync.Api.Services.Locations;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Moq;
using NUnit.Framework;

namespace EstateSync.Tests.Services;

[TestFixture]
public class LocationServiceTests
{
    private LocationService _service = null!;

    [SetUp]
    public void SetUp()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(item => item.ContentRootPath)
            .Returns(TestContext.CurrentContext.TestDirectory);
        _service = new LocationService(environment.Object);
    }

    [Test]
    public void GetStates_ReturnsAllStatesSortedByName()
    {
        var states = _service.GetStates();

        states.Should().HaveCount(36);
        states.Should().BeInAscendingOrder(item => item.Name);
        states.Should().Contain(item => item.Code == "TN" && item.Name == "Tamil Nadu");
    }

    [Test]
    public void SearchCities_PrioritizesPrefixMatchesAndHonorsLimit()
    {
        var cities = _service.SearchCities("TN", "chen", 5);

        cities.Should().NotBeEmpty();
        cities.Should().HaveCountLessThanOrEqualTo(5);
        cities[0].Name.Should().StartWithEquivalentOf("chen");
    }

    [TestCase("", "ch", 20)]
    [TestCase("T", "ch", 20)]
    [TestCase("123", "ch", 20)]
    [TestCase("ZZ", "ch", 20)]
    [TestCase("TN", "c", 20)]
    [TestCase("TN", "ch", 0)]
    [TestCase("TN", "ch", 51)]
    public void SearchCities_RejectsInvalidInput(string stateCode, string search, int limit)
    {
        var action = () => _service.SearchCities(stateCode, search, limit);

        action.Should().Throw<BadRequestException>();
    }

    [Test]
    public void SearchCities_IsCaseInsensitiveAndTrimsInput()
    {
        var cities = _service.SearchCities(" tn ", " CHEN ", 20);

        cities.Should().Contain(item => item.Name.Equals("Chennai", StringComparison.OrdinalIgnoreCase));
    }
}
