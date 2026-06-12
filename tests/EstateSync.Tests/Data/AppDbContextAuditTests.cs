using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using EstateSync.Api.Data;
using EstateSync.Api.Models;

namespace EstateSync.Tests.Data;

[TestFixture]
public class AppDbContextAuditTests
{
    private AppDbContext _context;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task SaveChangesAsync_ShouldCreateAuditLog_WhenEntityIsAdded()
    {
        // Arrange
        var role = new Role
        {
            RoleId = Guid.NewGuid(),
            Name = "AuditedRole"
        };

        // Act
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        // Assert
        var auditLogs = await _context.AuditLogs.ToListAsync();
        auditLogs.Should().HaveCount(1);
        
        var log = auditLogs.First();
        log.Action.Should().Be("CREATE");
        log.EntityType.Should().Be("Role");
        log.EntityId.Should().Be(role.RoleId.ToString());
        log.NewValues.Should().Contain(role.Name);
        log.OldValues.Should().BeNull();
    }

    [Test]
    public async Task SaveChangesAsync_ShouldCreateAuditLog_WhenEntityIsModified()
    {
        // Arrange
        var role = new Role
        {
            RoleId = Guid.NewGuid(),
            Name = "OriginalName"
        };
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();
        _context.AuditLogs.RemoveRange(_context.AuditLogs);
        await _context.SaveChangesAsync();

        // Act
        role.Name = "ModifiedName";
        _context.Roles.Update(role);
        await _context.SaveChangesAsync();

        // Assert
        var auditLogs = await _context.AuditLogs.ToListAsync();
        auditLogs.Should().HaveCount(1);

        var log = auditLogs.First();
        log.Action.Should().Be("UPDATE");
        log.EntityType.Should().Be("Role");
        log.EntityId.Should().Be(role.RoleId.ToString());
        log.OldValues.Should().Contain("OriginalName");
        log.NewValues.Should().Contain("ModifiedName");
    }

    [Test]
    public async Task SaveChangesAsync_ShouldCreateAuditLog_WhenEntityIsDeleted()
    {
        // Arrange
        var role = new Role
        {
            RoleId = Guid.NewGuid(),
            Name = "ToBeDeleted"
        };
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();
        _context.AuditLogs.RemoveRange(_context.AuditLogs);
        await _context.SaveChangesAsync();

        // Act
        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();

        // Assert
        var auditLogs = await _context.AuditLogs.ToListAsync();
        auditLogs.Should().HaveCount(1);

        var log = auditLogs.First();
        log.Action.Should().Be("DELETE");
        log.EntityType.Should().Be("Role");
        log.EntityId.Should().Be(role.RoleId.ToString());
        log.OldValues.Should().Contain("ToBeDeleted");
    }
}
