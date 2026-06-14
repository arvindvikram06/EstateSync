using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Data;
using EstateSync.Api.Models;
using EstateSync.Api.Interfaces.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;

namespace EstateSync.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class SystemController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPasswordService _passwordService;

    public SystemController(AppDbContext context, IPasswordService passwordService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
    }

    [HttpPost("reset-and-seed")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetAndSeed()
    {
        // 1. Delete all transactional records in order of FK dependencies
        await _context.Payments.ExecuteDeleteAsync();
        await _context.OwnerSettlements.ExecuteDeleteAsync();
        await _context.ComplaintImages.ExecuteDeleteAsync();
        await _context.Complaints.ExecuteDeleteAsync();
        await _context.RentIncreaseProposals.ExecuteDeleteAsync();
        await _context.LeaseBills.ExecuteDeleteAsync();
        await _context.LeaseDocuments.ExecuteDeleteAsync();
        await _context.PropertyDocuments.ExecuteDeleteAsync();
        await _context.OwnerVerificationDocuments.ExecuteDeleteAsync();
        await _context.TenantVerificationDocuments.ExecuteDeleteAsync();
        await _context.IdentityVerificationDocuments.ExecuteDeleteAsync();
        await _context.Documents.ExecuteDeleteAsync();
        await _context.Leases.ExecuteDeleteAsync();
        await _context.TenantVerificationRequests.ExecuteDeleteAsync();
        await _context.RentalApplications.ExecuteDeleteAsync();
        await _context.OwnerVerificationRequests.ExecuteDeleteAsync();
        await _context.IdentityVerificationRequests.ExecuteDeleteAsync();
        await _context.SavedProperties.ExecuteDeleteAsync();
        await _context.PropertyImages.ExecuteDeleteAsync();
        await _context.Properties.ExecuteDeleteAsync();
        await _context.OwnerProfiles.ExecuteDeleteAsync();
        await _context.StripeConnectedAccounts.ExecuteDeleteAsync();
        await _context.StripeCustomerProfiles.ExecuteDeleteAsync();
        await _context.RefreshTokens.ExecuteDeleteAsync();
        await _context.AuditLogs.ExecuteDeleteAsync();
        await _context.Notifications.ExecuteDeleteAsync();
        await _context.UserRoles.ExecuteDeleteAsync();
        await _context.Users.ExecuteDeleteAsync();

        // 2. Ensure standard roles exist
        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        if (adminRole == null)
        {
            adminRole = new Role { RoleId = Guid.NewGuid(), Name = "Admin", CreatedAt = DateTime.UtcNow };
            await _context.Roles.AddAsync(adminRole);
        }

        var ownerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Owner");
        if (ownerRole == null)
        {
            ownerRole = new Role { RoleId = Guid.NewGuid(), Name = "Owner", CreatedAt = DateTime.UtcNow };
            await _context.Roles.AddAsync(ownerRole);
        }

        var tenantRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Tenant");
        if (tenantRole == null)
        {
            tenantRole = new Role { RoleId = Guid.NewGuid(), Name = "Tenant", CreatedAt = DateTime.UtcNow };
            await _context.Roles.AddAsync(tenantRole);
        }
        await _context.SaveChangesAsync();

        // 3. Ensure a default location exists
        var location = await _context.Locations.FirstOrDefaultAsync();
        if (location == null)
        {
            foreach (var loc in EstateSync.Api.Data.Seeding.LocationSeedData.Locations)
            {
                var newLoc = new Location
                {
                    LocationId = Guid.NewGuid(),
                    StateName = loc.StateName,
                    District = loc.District,
                    Pincode = loc.Pincode,
                    Country = loc.Country
                };
                await _context.Locations.AddAsync(newLoc);
            }
            await _context.SaveChangesAsync();
            location = await _context.Locations.FirstOrDefaultAsync();
        }

        // 4. Seed Users
        // Admin
        var adminUser = new User
        {
            UserId = Guid.NewGuid(),
            FullName = "System Administrator",
            Email = "admin@estatesync.com",
            PhoneNumber = "1234567890",
            PasswordHash = _passwordService.HashPassword("Password123!"),
            IsActive = true,
            IsIdentityVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(adminUser);
        await _context.UserRoles.AddAsync(new UserRole { UserRoleId = Guid.NewGuid(), UserId = adminUser.UserId, RoleId = adminRole.RoleId });

        // Basic Owner
        var ownerUser = new User
        {
            UserId = Guid.NewGuid(),
            FullName = "Bob Owner",
            Email = "owner@estatesync.com",
            PhoneNumber = "2345678901",
            PasswordHash = _passwordService.HashPassword("Password123!"),
            IsActive = true,
            IsIdentityVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(ownerUser);
        await _context.UserRoles.AddAsync(new UserRole { UserRoleId = Guid.NewGuid(), UserId = ownerUser.UserId, RoleId = tenantRole.RoleId });

        // Basic Tenant
        var tenantUser = new User
        {
            UserId = Guid.NewGuid(),
            FullName = "Alice Tenant",
            Email = "tenant@estatesync.com",
            PhoneNumber = "3456789012",
            PasswordHash = _passwordService.HashPassword("Password123!"),
            IsActive = true,
            IsIdentityVerified = false,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(tenantUser);
        await _context.UserRoles.AddAsync(new UserRole { UserRoleId = Guid.NewGuid(), UserId = tenantUser.UserId, RoleId = tenantRole.RoleId });

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Database successfully reset and seeded with basic accounts!",
            admin = "admin@estatesync.com",
            owner = "owner@estatesync.com",
            tenant = "tenant@estatesync.com",
            defaultPassword = "Password123!"
        });
    }
}
