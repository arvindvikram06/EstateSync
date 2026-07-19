using EstateSync.Api.DTOs.Locations;

namespace EstateSync.Api.Interfaces.Services.Locations;

public interface ILocationService
{
    IReadOnlyList<StateResponse> GetStates();
    IReadOnlyList<CityResponse> SearchCities(string stateCode, string search, int limit);
}
