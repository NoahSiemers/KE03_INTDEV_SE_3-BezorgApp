using System.Text.Json;
using KE03_INTDEV_SE_3.Models;
using Microsoft.Maui.Devices.Sensors;

namespace KE03_INTDEV_SE_3.Services;

public class NavigationService
{
    private readonly HttpClient _httpClient;

    public NavigationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Location?> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
    {
        var request = new GeolocationRequest(
            GeolocationAccuracy.Best,
            TimeSpan.FromSeconds(10));

        #if IOS
                request.RequestFullAccuracy = true;
        #endif

        return await Geolocation.Default.GetLocationAsync(request, cancellationToken);
    }

    public async Task<Location?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        var locations = await Geocoding.Default.GetLocationsAsync(address);
        return locations?.FirstOrDefault();
    }

    public async Task<NavigationRoute?> BuildDrivingRouteAsync(
        Location origin,
        Location destination,
        CancellationToken cancellationToken = default)
    {
        var url =
            $"https://router.project-osrm.org/route/v1/driving/" +
            $"{origin.Longitude},{origin.Latitude};{destination.Longitude},{destination.Latitude}" +
            $"?overview=full&geometries=geojson&steps=true&alternatives=false";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var route = doc.RootElement.GetProperty("routes")[0];

        var path = new List<Location>();
        foreach (var coordinate in route.GetProperty("geometry").GetProperty("coordinates").EnumerateArray())
        {
            var longitude = coordinate[0].GetDouble();
            var latitude = coordinate[1].GetDouble();
            path.Add(new Location(latitude, longitude));
        }

        var steps = new List<NavigationStep>();
        foreach (var leg in route.GetProperty("legs").EnumerateArray())
        {
            foreach (var step in leg.GetProperty("steps").EnumerateArray())
            {
                var maneuver = step.GetProperty("maneuver");
                var type = maneuver.TryGetProperty("type", out var typeEl) ? typeEl.GetString() ?? "" : "";
                var modifier = maneuver.TryGetProperty("modifier", out var modEl) ? modEl.GetString() ?? "" : "";
                var roadName = step.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "" : "";

                steps.Add(new NavigationStep
                {
                    Instruction = BuildDutchInstruction(type, modifier, roadName),
                    DistanceMeters = step.GetProperty("distance").GetDouble(),
                    DurationSeconds = step.GetProperty("duration").GetDouble()
                });
            }
        }

        return new NavigationRoute
        {
            Path = path,
            Steps = steps,
            DistanceMeters = route.GetProperty("distance").GetDouble(),
            DurationSeconds = route.GetProperty("duration").GetDouble()
        };
    }

    private static string BuildDutchInstruction(string type, string modifier, string roadName)
    {
        string action = (type.ToLowerInvariant(), modifier.ToLowerInvariant()) switch
        {
            ("depart", _) => "Vertrek",
            ("arrive", _) => "Aankomst bij bestemming",
            ("turn", "left") => "Sla linksaf",
            ("turn", "slight left") => "Houd licht links aan",
            ("turn", "right") => "Sla rechtsaf",
            ("turn", "slight right") => "Houd licht rechts aan",
            ("roundabout", _) => "Neem de rotonde",
            ("new name", _) => "Ga verder op",
            _ => "Volg de route"
        };

        return string.IsNullOrWhiteSpace(roadName) ? action : $"{action} naar {roadName}";
    }
}