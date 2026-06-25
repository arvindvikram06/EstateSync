using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Data;
using EstateSync.Api.Models;
using EstateSync.Api.Interfaces.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace EstateSync.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class SystemController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IWebHostEnvironment _env;

    public SystemController(AppDbContext context, IPasswordService passwordService, IWebHostEnvironment env)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
        _env = env ?? throw new ArgumentNullException(nameof(env));
    }

    [HttpPost("reset-and-seed")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetAndSeed()
    {
        if (!_env.IsDevelopment())
            return NotFound();

        // 1. Delete all transactional records in order of FK dependencies
        await _context.Payments.ExecuteDeleteAsync();
        await _context.OwnerSettlements.ExecuteDeleteAsync();
        await _context.ComplaintImages.ExecuteDeleteAsync();
        await _context.Complaints.ExecuteDeleteAsync();
        await _context.RentIncreaseProposals.ExecuteDeleteAsync();
        await _context.LeaseBills.ExecuteDeleteAsync();
        await _context.LeaseDocuments.ExecuteDeleteAsync();
        await _context.PropertyDocuments.ExecuteDeleteAsync();
        await _context.TenantFinancialDocuments.ExecuteDeleteAsync();
        await _context.KycDocuments.ExecuteDeleteAsync();
        await _context.Documents.ExecuteDeleteAsync();
        await _context.Leases.ExecuteDeleteAsync();
        await _context.TenantFinancialProfiles.ExecuteDeleteAsync();
        await _context.RentalApplications.ExecuteDeleteAsync();
        await _context.KycVerifications.ExecuteDeleteAsync();
        await _context.SavedProperties.ExecuteDeleteAsync();
        await _context.PropertyImages.ExecuteDeleteAsync();
        await _context.Properties.ExecuteDeleteAsync();

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

        var verifierRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Verifier");
        if (verifierRole == null)
        {
            verifierRole = new Role { RoleId = Guid.NewGuid(), Name = "Verifier", CreatedAt = DateTime.UtcNow };
            await _context.Roles.AddAsync(verifierRole);
        }
        await _context.SaveChangesAsync();

        // 3. Ensure a default location exists
        var location = await _context.Locations.FirstOrDefaultAsync();
        if (location == null)
        {
            foreach (var loc in EstateSync.Api.Data.Seeding.LocationSeedData.Locations)
            {
                await _context.Locations.AddAsync(new Location
                {
                    LocationId = Guid.NewGuid(),
                    StateName = loc.StateName,
                    District = loc.District,
                    Pincode = loc.Pincode,
                    Country = loc.Country
                });
            }
            await _context.SaveChangesAsync();
            location = await _context.Locations.FirstOrDefaultAsync();
        }

        // 4. Seed Users
        var adminUser = new User
        {
            UserId = Guid.NewGuid(),
            FullName = "System Administrator",
            Email = "admin@estatesync.com",
            PhoneNumber = "1234567890",
            PasswordHash = _passwordService.HashPassword("Password123!"),
            IsActive = true,
            IsKycVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(adminUser);
        await _context.UserRoles.AddAsync(new UserRole { UserId = adminUser.UserId, RoleId = adminRole.RoleId });

        var ownerUser = new User
        {
            UserId = Guid.NewGuid(),
            FullName = "Bob Owner",
            Email = "owner@estatesync.com",
            PhoneNumber = "2345678901",
            PasswordHash = _passwordService.HashPassword("Password123!"),
            IsActive = true,
            IsKycVerified = true,
            IsOwnerVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(ownerUser);
        await _context.UserRoles.AddAsync(new UserRole { UserId = ownerUser.UserId, RoleId = ownerRole.RoleId });

        var tenantUser = new User
        {
            UserId = Guid.NewGuid(),
            FullName = "Alice Tenant",
            Email = "tenant@estatesync.com",
            PhoneNumber = "3456789012",
            PasswordHash = _passwordService.HashPassword("Password123!"),
            IsActive = true,
            IsKycVerified = false,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(tenantUser);
        await _context.UserRoles.AddAsync(new UserRole { UserId = tenantUser.UserId, RoleId = tenantRole.RoleId });

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Database successfully reset and seeded with basic accounts!",
            admin = "admin@estatesync.com",
            owner = "owner@estatesync.com",
            tenant = "tenant@estatesync.com",
            defaultPassword = "Password123!",
            locationId = location!.LocationId
        });
    }

    [HttpPost("seed-test-bill")]
    [AllowAnonymous]
    public async Task<IActionResult> SeedTestBill()
    {
        if (!_env.IsDevelopment())
            return NotFound();

        var lease = await _context.Leases.FirstOrDefaultAsync();
        if (lease == null)
            return BadRequest("No leases found. Please create a lease first.");

        var bill = new LeaseBill
        {
            LeaseBillId = Guid.NewGuid(),
            LeaseId = lease.LeaseId,
            BillType = LeaseBillType.Rent,
            DueDate = DateTime.UtcNow.AddDays(5),
            Amount = lease.MonthlyRent > 0 ? lease.MonthlyRent : 15000,
            PendingAmount = lease.MonthlyRent > 0 ? lease.MonthlyRent : 15000
        };

        await _context.LeaseBills.AddAsync(bill);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Test bill created successfully!",
            leaseBillId = bill.LeaseBillId,
            tenantId = lease.TenantId
        });
    }
}
