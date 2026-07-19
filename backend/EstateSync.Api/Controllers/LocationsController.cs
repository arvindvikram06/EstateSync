using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Locations;
using EstateSync.Api.Interfaces.Services.Locations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstateSync.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/locations")]
public sealed class LocationsController : ControllerBase
{
    private readonly ILocationService _locations;

    public LocationsController(ILocationService locations)
    {
        _locations = locations;
    }

    [HttpGet("states")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StateResponse>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyList<StateResponse>>> GetStates()
    {
        Response.Headers.CacheControl = "public,max-age=86400";
        return Ok(ApiResponse<IReadOnlyList<StateResponse>>.Ok(_locations.GetStates()));
    }

    [HttpGet("states/{stateCode}/cities")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CityResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse<IReadOnlyList<CityResponse>>> SearchCities(
        [FromRoute] string stateCode,
        [FromQuery] string search,
        [FromQuery] int limit = 20)
    {
        Response.Headers.CacheControl = "public,max-age=3600";
        return Ok(ApiResponse<IReadOnlyList<CityResponse>>.Ok(
            _locations.SearchCities(stateCode, search, limit)));
    }
}
