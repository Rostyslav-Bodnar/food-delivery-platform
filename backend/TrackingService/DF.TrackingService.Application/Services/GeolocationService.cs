using System.Globalization;
using System.Text.Json;
using DF.TrackingService.Contracts.Models.Responses;

namespace DF.TrackingService.Application.Services
{
    public class GeolocationService(HttpClient httpClient, string apiKey)
    {
        /// <summary>
        /// Forward geocoding: Convert address string to coordinates
        /// </summary>
        public async Task<GeodataResponse?> GetGeodataAsync(string address)
        {
            var url = $"https://geocode.maps.co/search?q={Uri.EscapeDataString(address)}&api_key={apiKey}";
            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
            {
                var first = doc.RootElement[0];

                if (double.TryParse(first.GetProperty("lat").GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) &&
                    double.TryParse(first.GetProperty("lon").GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
                {
                    return new GeodataResponse(lat, lon);
                }

            }

            return null;
        }


        /// <summary>
        /// Reverse geocoding: Convert coordinates to address
        /// </summary>
        public async Task<string?> GetAddressAsync(double lat, double lon)
        {
            var url = $"https://geocode.maps.co/reverse?lat={lat}&lon={lon}&api_key={apiKey}";
            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("display_name", out var displayName))
            {
                return displayName.GetString();
            }

            return null;
        }
    }
}
