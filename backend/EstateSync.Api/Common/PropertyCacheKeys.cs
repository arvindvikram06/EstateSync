using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using EstateSync.Api.DTOs.Properties;

namespace EstateSync.Api.Common;

public static class PropertyCacheKeys
{
    public const string ListingsVersion = "estatesync:property-listings:version";

    public static string Search(PropertySearchRequest request, long version)
    {
        var canonical = string.Join('|',
            request.Search?.Trim().ToLowerInvariant() ?? "",
            request.State?.Trim().ToLowerInvariant() ?? "",
            request.ListingType?.Trim().ToLowerInvariant() ?? "",
            request.PropertyType?.Trim().ToLowerInvariant() ?? "",
            request.Bedrooms?.ToString(CultureInfo.InvariantCulture) ?? "",
            request.MinRent?.ToString(CultureInfo.InvariantCulture) ?? "",
            request.MaxRent?.ToString(CultureInfo.InvariantCulture) ?? "",
            request.Page.ToString(CultureInfo.InvariantCulture),
            request.Limit.ToString(CultureInfo.InvariantCulture));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        return $"estatesync:property-listings:v{version}:{hash}";
    }
}
