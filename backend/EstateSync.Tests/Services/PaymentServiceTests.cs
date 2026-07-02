using System;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Services.Payments;

namespace EstateSync.Tests.Services;

[TestFixture]
public class PaymentServiceTests
{
    private Mock<IUserRepository> _userRepoMock;
    private Mock<ILeaseBillRepository> _leaseBillRepoMock;
    private Mock<IStripeConnectedAccountRepository> _stripeAccountRepoMock;
    private Mock<IStripeCustomerProfileRepository> _stripeProfileRepoMock;
    private Mock<IPaymentRepository> _paymentRepoMock;
    private Mock<IOwnerSettlementRepository> _ownerSettlementRepoMock;
    private Mock<INotificationRepository> _notificationRepoMock;
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private Mock<IConfiguration> _configMock;
    private Mock<ILogger<PaymentService>> _loggerMock;
    private PaymentService _service;

    [SetUp]
    public void Setup()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _leaseBillRepoMock = new Mock<ILeaseBillRepository>();
        _stripeAccountRepoMock = new Mock<IStripeConnectedAccountRepository>();
        _stripeProfileRepoMock = new Mock<IStripeCustomerProfileRepository>();
        _paymentRepoMock = new Mock<IPaymentRepository>();
        _ownerSettlementRepoMock = new Mock<IOwnerSettlementRepository>();
        _notificationRepoMock = new Mock<INotificationRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _configMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<PaymentService>>();

        _service = new PaymentService(
            _userRepoMock.Object,
            _leaseBillRepoMock.Object,
            _stripeAccountRepoMock.Object,
            _stripeProfileRepoMock.Object,
            _paymentRepoMock.Object,
            _ownerSettlementRepoMock.Object,
            _notificationRepoMock.Object,
            _unitOfWorkMock.Object,
            _configMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public void Constructor_ShouldInstantiateSuccessfully()
    {
        _service.Should().NotBeNull();
    }
}
