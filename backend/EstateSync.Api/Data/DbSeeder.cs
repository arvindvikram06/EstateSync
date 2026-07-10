using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using EstateSync.Api.Interfaces.Services.Auth;
using EstateSync.Api.Models;
using EstateSync.Api.Models.Enums;

namespace EstateSync.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<AppDbContext>();
        var env = services.GetRequiredService<IHostEnvironment>();
        var passwordService = services.GetRequiredService<IPasswordService>();

        // Roles and a system admin already exist via EF Core HasData migration seed.
        // Guard on properties (not users) so we don't bail out prematurely.
        if (await context.Properties.AnyAsync()) return;

        Console.WriteLine("[DbSeeder] No properties found — seeding test data...");

        CopySeedAssets(env);

        var now = DateTime.UtcNow;
        var passwordHash = passwordService.HashPassword("Password123!");

        // ── Roles — already seeded via HasData, load from DB ─────────────────
        var adminRole  = await context.Roles.SingleAsync(r => r.Name == "Admin");
        var ownerRole  = await context.Roles.SingleAsync(r => r.Name == "Owner");
        var tenantRole = await context.Roles.SingleAsync(r => r.Name == "Tenant");

        // ── Admin ──────────────────────────────────────────────────────────────
        var admin = MakeUser("Arun Admin", "arvindvikramtest+admin@gmail.com", passwordHash, now, "9000000001");
        context.Users.Add(admin);
        context.UserRoles.Add(new UserRole { UserRoleId = Guid.NewGuid(), UserId = admin.UserId, RoleId = adminRole.RoleId });

        // ── Owners ────────────────────────────────────────────────────────────
        var owner1 = MakeUser("Ravi Kumar",   "arvindvikramtest+owner1@gmail.com", passwordHash, now, "9000000002", isKyc: true, isOwner: true);
        var owner2 = MakeUser("Suresh Patel", "arvindvikramtest+owner2@gmail.com", passwordHash, now, "9000000003", isKyc: true, isOwner: true);
        var owner3 = MakeUser("Anand Sharma", "arvindvikramtest+owner3@gmail.com", passwordHash, now, "9000000004", isKyc: true, isOwner: true);
        context.Users.AddRange(owner1, owner2, owner3);

        foreach (var o in new[] { owner1, owner2, owner3 })
        {
            context.UserRoles.Add(new UserRole { UserRoleId = Guid.NewGuid(), UserId = o.UserId, RoleId = ownerRole.RoleId });
            context.OwnerProfiles.Add(new OwnerProfile
            {
                OwnerProfileId = Guid.NewGuid(),
                UserId = o.UserId,
                OwnerType = OwnerType.Individual,
                OwnerSince = now,
                CreatedAt = now
            });
        }

        // Stripe connected account for owner1 only
        context.StripeConnectedAccounts.Add(new StripeConnectedAccount
        {
            Id = Guid.NewGuid(),
            OwnerId = owner1.UserId,
            StripeAccountId = "acct_1TpQumDO3esnlq00",
            ChargesEnabled = true,
            PayoutsEnabled = true,
            DetailsSubmitted = true,
            LastSyncedAt = now,
            CreatedAt = now
        });

        // ── Tenants ───────────────────────────────────────────────────────────
        var tenant1 = MakeUser("Priya Nair",  "arvindvikramtest+tenant1@gmail.com", passwordHash, now, "9000000005", isKyc: true);
        var tenant2 = MakeUser("Arun Menon",  "arvindvikramtest+tenant2@gmail.com", passwordHash, now, "9000000006", isKyc: true);
        var tenant3 = MakeUser("Divya Reddy", "arvindvikramtest+tenant3@gmail.com", passwordHash, now, "9000000007", isKyc: true);
        var tenant4 = MakeUser("Karthik Raj", "arvindvikramtest+tenant4@gmail.com", passwordHash, now, "9000000008", isKyc: true);
        var tenant5 = MakeUser("Meena Iyer",  "arvindvikramtest+tenant5@gmail.com", passwordHash, now, "9000000009", isKyc: true);
        context.Users.AddRange(tenant1, tenant2, tenant3, tenant4, tenant5);

        foreach (var t in new[] { tenant1, tenant2, tenant3, tenant4, tenant5 })
            context.UserRoles.Add(new UserRole { UserRoleId = Guid.NewGuid(), UserId = t.UserId, RoleId = tenantRole.RoleId });

        // Stripe customer profile for tenant1 only
        context.StripeCustomerProfiles.Add(new StripeCustomerProfile
        {
            Id = Guid.NewGuid(),
            TenantId = tenant1.UserId,
            StripeCustomerId = "cus_Up90nxsT9dcG37",
            CreatedAt = now
        });

        // ── KYC for all owners + tenants ──────────────────────────────────────
        var kycUsers = new[] { owner1, owner2, owner3, tenant1, tenant2, tenant3, tenant4, tenant5 };
        var kycDocTypes = new[] { DocumentType.AadhaarFront, DocumentType.AadhaarBack, DocumentType.PanCard, DocumentType.AddressProof };
        const string kycDocUrl = "/uploads/properties/seed/lease.pdf";

        // Unique Aadhaar (12 digits) and PAN (AAAAA9999A format) per user
        var aadhaarNumbers = new[] { "234567890123", "345678901234", "456789012345", "567890123456", "678901234567", "789012345678", "890123456789", "901234567890" };
        var panNumbers     = new[] { "ABCDE1234F", "BCDEF2345G", "CDEFG3456H", "DEFGH4567I", "EFGHI5678J", "FGHIJ6789K", "GHIJK7890L", "HIJKL8901M" };

        for (int i = 0; i < kycUsers.Length; i++)
        {
            var user = kycUsers[i];
            var kyc = new KycVerification
            {
                KycVerificationId = Guid.NewGuid(),
                UserId = user.UserId,
                AadhaarNumber = aadhaarNumbers[i],
                PanNumber = panNumbers[i],
                Status = ApprovalStatus.Approved,
                ReviewedById = admin.UserId,
                ReviewedAt = now,
                ReviewRemarks = "Seeded — auto-approved",
                CreatedAt = now
            };
            context.KycVerifications.Add(kyc);

            foreach (var docType in kycDocTypes)
            {
                var doc = new Document
                {
                    DocumentId = Guid.NewGuid(),
                    DocumentType = docType,
                    DocumentUrl = kycDocUrl,
                    OriginalFileName = "kyc-doc.pdf",
                    MimeType = "application/pdf",
                    FileSize = 102400,
                    UploadedById = user.UserId,
                    UploadedAt = now,
                    ReviewStatus = ApprovalStatus.Approved,
                    ReviewedAt = now,
                    ReviewedById = admin.UserId,
                    CreatedAt = now
                };
                context.Documents.Add(doc);
                context.KycDocuments.Add(new KycDocument
                {
                    KycVerificationId = kyc.KycVerificationId,
                    DocumentId = doc.DocumentId,
                    CreatedAt = now
                });
            }
        } // end for kycUsers

        // ── Shared property deed document ─────────────────────────────────────
        var deedDoc = new Document
        {
            DocumentId = Guid.NewGuid(),
            DocumentType = DocumentType.PropertyDeed,
            DocumentUrl = "/uploads/properties/seed/lease.pdf",
            OriginalFileName = "property-deed.pdf",
            MimeType = "application/pdf",
            FileSize = 102400,
            UploadedById = admin.UserId,
            UploadedAt = now,
            ReviewStatus = ApprovalStatus.Approved,
            ReviewedAt = now,
            ReviewedById = admin.UserId,
            CreatedAt = now
        };
        context.Documents.Add(deedDoc);

        // ── Properties ────────────────────────────────────────────────────────
        var properties = new List<Property>
        {
            // Tamil Nadu — Chennai
            MakeProp("Serene Villa in Adyar", "A tranquil villa nestled in the heart of Adyar with lush greenery.", owner1, PropertyType.Villa, PropertyStatus.Active,
                "Tamil Nadu", "Chennai", "600020", "12 Lattice Bridge Rd, Adyar", 13.0012m, 80.2565m, 5, 4, 55000, 110000, new[]{"Parking","Power Backup","Security","Garden","Swimming Pool"}, admin, now),

            MakeProp("Independent House in Anna Nagar", "Well-maintained 3BHK independent house in prime Anna Nagar location.", owner1, PropertyType.IndependentHouse, PropertyStatus.Active,
                "Tamil Nadu", "Chennai", "600040", "45 2nd Ave, Anna Nagar", 13.0891m, 80.2089m, 4, 3, 38000, 76000, new[]{"Parking","Power Backup","Security"}, admin, now),

            MakeProp("Spacious Home in Velachery", "Bright and airy 2BHK home with easy access to OMR tech corridor.", owner2, PropertyType.IndependentHouse, PropertyStatus.Active,
                "Tamil Nadu", "Chennai", "600042", "8 Taramani Link Rd, Velachery", 12.9815m, 80.2180m, 3, 2, 28000, 56000, new[]{"Parking","Power Backup","Security"}, admin, now),

            MakeProp("Luxury Villa in Nungambakkam", "Elegant 5BHK villa in the most coveted address in Chennai.", owner2, PropertyType.Villa, PropertyStatus.Active,
                "Tamil Nadu", "Chennai", "600034", "3 Khader Nawaz Khan Rd, Nungambakkam", 13.0545m, 80.2419m, 6, 5, 80000, 160000, new[]{"Parking","Power Backup","Security","Garden","Swimming Pool","CCTV"}, admin, now),

            // Tamil Nadu — Coimbatore
            MakeProp("Cozy Home in RS Puram", "Peaceful 2BHK home in RS Puram, ideal for small families.", owner1, PropertyType.IndependentHouse, PropertyStatus.Active,
                "Tamil Nadu", "Coimbatore", "641002", "15 DB Rd, RS Puram", 11.0168m, 76.9558m, 3, 2, 22000, 44000, new[]{"Parking","Power Backup"}, admin, now),

            MakeProp("Villa in Saravanampatti", "Modern 3BHK villa close to IT parks and Coimbatore international airport.", owner2, PropertyType.Villa, PropertyStatus.Active,
                "Tamil Nadu", "Coimbatore", "641035", "22 Kalapatti Rd, Saravanampatti", 11.0680m, 77.0156m, 4, 3, 42000, 84000, new[]{"Parking","Power Backup","Security","Garden"}, admin, now),

            MakeProp("Independent House in Peelamedu", "Spacious 3BHK house close to PSG Tech and Tidel Park Coimbatore.", owner3, PropertyType.IndependentHouse, PropertyStatus.Active,
                "Tamil Nadu", "Coimbatore", "641004", "7 Avinashi Rd, Peelamedu", 11.0300m, 77.0017m, 3, 3, 30000, 60000, new[]{"Parking","Power Backup","Security"}, admin, now),

            // Karnataka — Bangalore
            MakeProp("Premium Villa in Whitefield", "Gated-community 4BHK villa in Whitefield, walking distance to ITPL.", owner2, PropertyType.Villa, PropertyStatus.Active,
                "Karnataka", "Bangalore", "560066", "18 ITPL Main Rd, Whitefield", 12.9698m, 77.7500m, 5, 4, 70000, 140000, new[]{"Parking","Power Backup","Security","Garden","Swimming Pool","Gym"}, admin, now),

            MakeProp("Independent House in Indiranagar", "Classic 3BHK independent house in one of Bangalore's most sought-after neighbourhoods.", owner1, PropertyType.IndependentHouse, PropertyStatus.Active,
                "Karnataka", "Bangalore", "560038", "56 12th Main, Indiranagar", 12.9784m, 77.6408m, 4, 3, 45000, 90000, new[]{"Parking","Power Backup","Security","Garden"}, admin, now),

            MakeProp("Cozy Home in Koramangala", "Well-connected 2BHK home in Koramangala, close to cafes and tech offices.", owner3, PropertyType.IndependentHouse, PropertyStatus.Active,
                "Karnataka", "Bangalore", "560034", "9 80 Feet Rd, Koramangala", 12.9352m, 77.6245m, 3, 2, 32000, 64000, new[]{"Parking","Power Backup","Security"}, admin, now),

            MakeProp("Spacious Villa in Jayanagar", "Grand 5BHK villa in Jayanagar, surrounded by parks and excellent schools.", owner3, PropertyType.Villa, PropertyStatus.Active,
                "Karnataka", "Bangalore", "560041", "4 30th Cross, Jayanagar", 12.9308m, 77.5833m, 6, 5, 90000, 180000, new[]{"Parking","Power Backup","Security","Garden","Swimming Pool","CCTV","Gym"}, admin, now),

            // Andhra Pradesh
            MakeProp("Modern Villa in Gachibowli", "Contemporary 4BHK villa in Gachibowli's tech hub, close to Hyderabad Financial District.", owner1, PropertyType.Villa, PropertyStatus.Active,
                "Andhra Pradesh", "Hyderabad", "500032", "14 Nanakramguda Rd, Gachibowli", 17.4401m, 78.3489m, 5, 4, 65000, 130000, new[]{"Parking","Power Backup","Security","Garden","Swimming Pool"}, admin, now),

            MakeProp("Independent House in Banjara Hills", "Elegant 3BHK house in prestigious Banjara Hills, Hyderabad.", owner2, PropertyType.IndependentHouse, PropertyStatus.Active,
                "Andhra Pradesh", "Hyderabad", "500034", "22 Road No 10, Banjara Hills", 17.4156m, 78.4483m, 4, 3, 48000, 96000, new[]{"Parking","Power Backup","Security","Garden"}, admin, now),

            MakeProp("Home in MVP Colony", "Comfortable 2BHK home in MVP Colony, minutes from Vizag beach.", owner3, PropertyType.IndependentHouse, PropertyStatus.Active,
                "Andhra Pradesh", "Visakhapatnam", "530017", "5 Sector 7, MVP Colony", 17.7231m, 83.3012m, 3, 2, 20000, 40000, new[]{"Parking","Power Backup"}, admin, now),

            MakeProp("Villa in Vijayawada", "Charming 3BHK villa in Vijayawada, near Krishna River waterfront.", owner1, PropertyType.Villa, PropertyStatus.Active,
                "Andhra Pradesh", "Vijayawada", "520010", "9 MG Rd, Vijayawada", 16.5062m, 80.6480m, 4, 3, 35000, 70000, new[]{"Parking","Power Backup","Security","Garden"}, admin, now),
        };

        context.Properties.AddRange(properties);

        // Images + property documents
        foreach (var prop in properties)
        {
            context.PropertyImages.AddRange(
                new PropertyImage { ImageId = Guid.NewGuid(), PropertyId = prop.PropertyId, Image = "/uploads/properties/seed/house.jpeg",  IsPrimary = true,  DisplayOrder = 1, CreatedAt = now },
                new PropertyImage { ImageId = Guid.NewGuid(), PropertyId = prop.PropertyId, Image = "/uploads/properties/seed/house2.jpeg", IsPrimary = false, DisplayOrder = 2, CreatedAt = now }
            );
            context.PropertyDocuments.Add(new PropertyDocument
            {
                PropertyDocumentId = Guid.NewGuid(),
                PropertyId = prop.PropertyId,
                DocumentId = deedDoc.DocumentId,
                CreatedAt = now
            });
        }

        await context.SaveSeedDataAsync();
        Console.WriteLine($"[DbSeeder] Done — seeded {properties.Count} properties, {kycUsers.Length * 4} KYC docs, 9 users.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static User MakeUser(string name, string email, string hash, DateTime now, string phone,
        bool isKyc = false, bool isOwner = false) => new()
    {
        UserId = Guid.NewGuid(),
        FullName = name,
        Email = email,
        PhoneNumber = phone,
        PasswordHash = hash,
        IsActive = true,
        IsEmailVerified = true,
        IsKycVerified = isKyc,
        IsOwnerVerified = isOwner,
        CreatedAt = now
    };

    private static Property MakeProp(
        string title, string description,
        User owner, PropertyType type, PropertyStatus status,
        string state, string city, string pincode, string address,
        decimal lat, decimal lng,
        int bathrooms, int bedrooms,
        decimal rent, decimal deposit,
        string[] amenities,
        User approver, DateTime now) => new()
    {
        PropertyId = Guid.NewGuid(),
        OwnerId = owner.UserId,
        Title = title,
        Description = description,
        PropertyType = type,
        Status = status,
        State = state,
        City = city,
        Pincode = pincode,
        AddressLine = address,
        Latitude = lat,
        Longitude = lng,
        Bathrooms = bathrooms,
        Bedrooms = bedrooms,
        MonthlyRent = rent,
        SecurityDeposit = deposit,
        LandArea = 2400,
        BuildupArea = 1800,
        Amenities = amenities.ToList(),
        ApproveById = approver.UserId,
        ApproveRemarks = "Seeded — auto-approved",
        CreatedAt = now
    };

    private static void CopySeedAssets(IHostEnvironment env)
    {
        var src = Path.Combine(env.ContentRootPath, "SeedAssets");
        var dst = Path.Combine(env.ContentRootPath, "uploads", "properties", "seed");
        Directory.CreateDirectory(dst);

        foreach (var file in Directory.GetFiles(src))
        {
            var destFile = Path.Combine(dst, Path.GetFileName(file));
            if (!File.Exists(destFile))
                File.Copy(file, destFile);
        }
    }
}
