using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Data;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.Payments;
using EstateSync.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EstateSync.Api.Services.Payments;

public class PlatformFeeService : IPlatformFeeService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;

    public PlatformFeeService(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<decimal> GetPercentAsync(CancellationToken ct = default)
    {
        var stored = await _db.PlatformSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Key == PlatformSetting.PlatformFeePercentKey, ct);

        if (stored != null)
            return stored.DecimalValue;

        return decimal.TryParse(
            _configuration["Stripe:PlatformFeePercent"],
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var configured)
            ? configured
            : 0.02m;
    }

    public async Task<decimal> UpdatePercentAsync(
        decimal percentage, Guid adminUserId, CancellationToken ct = default)
    {
        if (percentage is < 0m or > 25m)
            throw new BadRequestException("Platform charge must be between 0% and 25%.");

        var fraction = decimal.Round(percentage / 100m, 6, MidpointRounding.AwayFromZero);
        var setting = await _db.PlatformSettings
            .SingleOrDefaultAsync(x => x.Key == PlatformSetting.PlatformFeePercentKey, ct);

        if (setting == null)
        {
            setting = new PlatformSetting { Key = PlatformSetting.PlatformFeePercentKey };
            _db.PlatformSettings.Add(setting);
        }

        setting.DecimalValue = fraction;
        setting.UpdatedAt = DateTime.UtcNow;
        setting.UpdatedByUserId = adminUserId;
        await _db.SaveChangesAsync(ct);
        return fraction * 100m;
    }
}
