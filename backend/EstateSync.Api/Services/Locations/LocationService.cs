using System.Text.Json;
using EstateSync.Api.DTOs.Locations;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.Locations;

namespace EstateSync.Api.Services.Locations;

public sealed class LocationService : ILocationService
{
    private const int MinSearchLength = 2;
    private const int MaxSearchLength = 100;
    private const int MaxLimit = 50;

    private readonly IReadOnlyList<StateResponse> _states;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _citiesByState;

    public LocationService(IWebHostEnvironment environment)
    {
        var path = Path.Combine(environment.ContentRootPath, "Data", "Locations", "india-locations.json");
        if (!File.Exists(path))
            throw new InvalidOperationException($"Location data file was not found at '{path}'.");

        using var stream = File.OpenRead(path);
        var data = JsonSerializer.Deserialize<LocationData>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Location data could not be deserialized.");

        if (data.States.Count == 0)
            throw new InvalidOperationException("Location data contains no states.");

        _states = data.States
            .Select(state => new StateResponse(state.Code, state.Name))
            .OrderBy(state => state.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _citiesByState = data.States.ToDictionary(
            state => state.Code,
            state => (IReadOnlyList<string>)state.Cities
                .Where(city => !string.IsNullOrWhiteSpace(city))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(city => city, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<StateResponse> GetStates() => _states;

    public IReadOnlyList<CityResponse> SearchCities(string stateCode, string search, int limit)
    {
        var normalizedStateCode = stateCode?.Trim() ?? string.Empty;
        if (normalizedStateCode.Length != 2 || !normalizedStateCode.All(char.IsLetter))
            throw new BadRequestException("State code must contain exactly two letters.");

        if (!_citiesByState.TryGetValue(normalizedStateCode, out var cities))
            throw new BadRequestException("Unknown Indian state code.");

        var normalizedSearch = search?.Trim() ?? string.Empty;
        if (normalizedSearch.Length < MinSearchLength)
            throw new BadRequestException($"City search must contain at least {MinSearchLength} characters.");
        if (normalizedSearch.Length > MaxSearchLength)
            throw new BadRequestException($"City search cannot exceed {MaxSearchLength} characters.");
        if (limit is < 1 or > MaxLimit)
            throw new BadRequestException($"Limit must be between 1 and {MaxLimit}.");

        return cities
            .Where(city => city.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
            .OrderBy(city => city.StartsWith(normalizedSearch, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(city => city, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(city => new CityResponse(city))
            .ToArray();
    }

    private sealed record LocationData(List<StateData> States);
    private sealed record StateData(string Code, string Name, List<string> Cities);
}
