using System.Net.Http.Json;
using DF.UserService.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace DF.UserService.Application.Services;

public class PromoService(HttpClient httpClient, IConfiguration config) : IPromoService
{
    private readonly string _baseUrl = config.GetValue<string>("ExternalServices:PromoServiceApiUrl")
                                       ?? throw new InvalidOperationException("Promo API URL not configured");

    public async Task AssignWelcomePromoAsync(Guid accountId, string email)
    {
        var requestBody = new
        {
            promo = new
            {
                code = $"WELCOME-{accountId.ToString()[..8].ToUpper()}",
                discount_percent = 10,
                valid_from = DateTime.UtcNow,
                valid_until = DateTime.UtcNow.AddDays(30),
                is_global = false,
                usage_limit = 1
            }
        };

        var response = await httpClient.PostAsJsonAsync($"{_baseUrl}/api/promos", requestBody);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"PromoService error: {response.StatusCode} - {error}");
        }
    }
}