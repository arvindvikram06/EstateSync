using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Data.Seeding;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data;

public static class DbSeeder
{
    // public static async Task SeedLocationsAsync(AppDbContext context)
    // {
    //     if (!await context.Locations.AnyAsync())
    //     {
    //         await context.Locations.AddRangeAsync(LocationSeedData.Locations);
    //         await context.SaveChangesAsync();
    //     }
    // }

    public static async Task SeedAgreementTypesAsync(AppDbContext context)
    {
        if (!await context.AgreementTypes.AnyAsync())
        {
            var types = new[]
            {
                new AgreementType
                {
                    AgreementTypeId = new Guid("9a82643a-4467-4f65-9856-d76b1f234501"),
                    Name = "Residential Lease Agreement",
                    Description = "Standard agreement for residential tenancy. Rent increases are not allowed during the lease term.",
                    AllowsRentIncrease = false,
                    RequiresNoticePeriod = true,
                    DefaultPenaltyPercentage = 5.0m,
                    CreatedAt = DateTime.UtcNow
                },
                new AgreementType
                {
                    AgreementTypeId = new Guid("9a82643a-4467-4f65-9856-d76b1f234502"),
                    Name = "Rent Agreement",
                    Description = "A rent agreement that allows the owner to propose a rent increase during the lease term.",
                    AllowsRentIncrease = true,
                    RequiresNoticePeriod = true,
                    DefaultPenaltyPercentage = 5.0m,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.AgreementTypes.AddRangeAsync(types);
            await context.SaveChangesAsync();
        }
    }

}
