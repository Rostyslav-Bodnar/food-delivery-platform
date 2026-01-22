using System.Text.Json;
using DF.TrackingService.Application.Services.Interfaces;
using DF.TrackingService.Contracts.Models.Responses;
using Microsoft.Extensions.Configuration;

namespace DF.TrackingService.Application.Services;

public class RoutingService(HttpClient http, IConfiguration cfg) : IRoutingService
{
    private readonly string osrmBase = cfg["Osrm:BaseUrl"]!; // e.g. http://router.project-osrm.org

    public async Task<RouteResult> BuildRouteAsync(RoutePoint courier, RoutePoint business, RoutePoint customer)
    {
        var coords = $"{courier.Longitude},{courier.Latitude};{business.Longitude},{business.Latitude};{customer.Longitude},{customer.Latitude}";
        var url = $"{osrmBase}/route/v1/driving/{coords}?overview=full&geometries=geojson&steps=false&annotations=duration,distance";

        var resp = await http.GetAsync(url);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var routes = root.GetProperty("routes");
        var route = routes[0];

        var distance = route.GetProperty("distance").GetDouble();    // meters
        var duration = route.GetProperty("duration").GetDouble();    // seconds
        var geometry = route.GetProperty("geometry").GetRawText();   // GeoJSON LineString

        var legs = route.GetProperty("legs");
        var legDistances = new List<double>(legs.GetArrayLength());
        var legDurations = new List<double>(legs.GetArrayLength());
        foreach (var leg in legs.EnumerateArray())
        {
            legDistances.Add(leg.GetProperty("distance").GetDouble());
            legDurations.Add(leg.GetProperty("duration").GetDouble());
        }

        return new RouteResult(distance, duration, geometry, legDistances, legDurations);
    }

    public async Task<RouteResult?> TryRebuildIfDeviatedAsync(Guid orderId, RoutePoint currentCourier, double thresholdMeters)
    {
        // 1) дістаємо останню маршрутну лінію та заплановану точку збережену для orderId
        // 2) визначаємо відстань до маршруту (snap-to або nearest service)
        // 3) якщо > thresholdMeters — перераховуємо BuildRouteAsync з поточної позиції
        return null;
    }
}
