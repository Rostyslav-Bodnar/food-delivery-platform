using System.Globalization;
using System.Text.Json;
using DF.OrderService.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace DF.OrderService.Application.Services;

public class OsrmDistanceService(HttpClient http, IConfiguration cfg) : IDistanceService
{
    private readonly string osrmBase = cfg["Osrm:BaseUrl"]!; // наприклад: http://router.project-osrm.org

    public async Task<double> GetDistanceKmAsync(double fromLat, double fromLon, double toLat, double toLon)
    {
        var coords = string.Format(
            CultureInfo.InvariantCulture, "{0},{1};{2},{3}", fromLon, fromLat, toLon, toLat);
        var url = $"{osrmBase}/route/v1/driving/{coords}?overview=false";

        var resp = await http.GetAsync(url);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        var route = doc.RootElement.GetProperty("routes")[0];
        var distanceMeters = route.GetProperty("distance").GetDouble();

        return distanceMeters / 1000.0; // повертаємо км
    }
}