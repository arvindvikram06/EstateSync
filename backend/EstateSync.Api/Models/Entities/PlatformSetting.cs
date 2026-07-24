using System;

namespace EstateSync.Api.Models;

public class PlatformSetting
{
    public const string PlatformFeePercentKey = "PlatformFeePercent";

    public string Key { get; set; } = string.Empty;
    public decimal DecimalValue { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}
