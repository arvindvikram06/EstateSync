using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using EstateSync.Api.DTOs.AuditLogs;
using EstateSync.Api.Mappers;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Services.AuditLogs;

namespace EstateSync.Tests.Services;

[TestFixture]
public class AuditLogServiceTests
{
    private Mock<IAuditLogRepository> _auditLogRepoMock;
    private AuditLogMapper _mapper;
    private Mock<ILogger<AuditLogService>> _loggerMock;
    private AuditLogService _service;

    [SetUp]
    public void SetUp()
    {
        _auditLogRepoMock = new Mock<IAuditLogRepository>();
        _mapper = new AuditLogMapper();
        _loggerMock = new Mock<ILogger<AuditLogService>>();
        
        _service = new AuditLogService(
            _auditLogRepoMock.Object,
            _mapper,
            _loggerMock.Object
        );
    }

    [Test]
    public async Task GetPagedLogsAsync_ShouldReturnPagedResponse_WithMappedItems()
    {
        // Arrange
        var logs = new List<AuditLog>
        {
            new AuditLog
            {
                AuditLogId = 1,
                EntityId = "guid-1",
                EntityType = "Property",
                Action = "CREATE",
                NewValues = "{}",
                CreatedAt = DateTime.UtcNow
            },
            new AuditLog
            {
                AuditLogId = 2,
                EntityId = "guid-2",
                EntityType = "Property",
                Action = "UPDATE",
                NewValues = "{}",
                OldValues = "{}",
                CreatedAt = DateTime.UtcNow
            }
        };

        _auditLogRepoMock
            .Setup(x => x.GetPagedAsync("Property", null, null, 1, 10))
            .ReturnsAsync((logs, 2));

        // Act
        var result = await _service.GetPagedLogsAsync("Property", null, null, 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Page.Should().Be(1);
        result.Limit.Should().Be(10);
        result.Total.Should().Be(2);
        result.Data.Should().HaveCount(2);

        var first = result.Data.First();
        first.AuditLogId.Should().Be(1);
        first.EntityId.Should().Be("guid-1");
        first.EntityType.Should().Be("Property");
        first.Action.Should().Be("CREATE");
    }
}
