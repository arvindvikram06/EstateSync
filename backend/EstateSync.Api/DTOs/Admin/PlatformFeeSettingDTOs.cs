using System.ComponentModel.DataAnnotations;

namespace EstateSync.Api.DTOs.Admin;

public record PlatformFeeSettingResponse(decimal Percentage);

public record UpdatePlatformFeeSettingRequest(
    [property: Range(typeof(decimal), "0", "25",
        ErrorMessage = "Platform charge must be between 0% and 25%.")]
    decimal Percentage);
