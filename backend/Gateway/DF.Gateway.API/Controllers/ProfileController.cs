using System.Security.Claims;
using DF.Contracts.Gateway.Responses;
using DF.Gateway.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DF.Gateway.API.Controllers;

[Authorize]
[ApiController]
[Route("api/profile")]
public class ProfileController(InternalHttpClient httpClient) : ControllerBase
{
    // =========================
    // GET PROFILE
    // =========================
    [HttpGet]
    public async Task<ActionResult<Response<ProfileResponse>>> GetProfile()
        => await Forward<ProfileResponse>("/api/profile");

    // =========================
    // SWITCH ACCOUNT
    // =========================
    [HttpPut("switch/{accountId:guid}")]
    public async Task<ActionResult<Response<object>>> SwitchAccount(Guid accountId)
        => await Forward<object>($"/api/profile/switch/{accountId}", HttpMethod.Put);

    // =========================
    // INTERNAL
    // =========================
    private async Task<ActionResult<Response<T>>> Forward<T>(
        string url,
        HttpMethod? method = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Ok(new Response<T>(
                false,
                default,
                "User not authenticated"
            ));
        }

        var request = new HttpRequestMessage(
            method ?? HttpMethod.Get,
            url);

        var response = await httpClient.SendAsync(request, userId);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content
                .ReadFromJsonAsync<ServiceErrorResponse>();

            return Ok(new Response<T>(
                false,
                default,
                error?.Message ?? "Profile error"
            ));
        }

        var data = await response.Content.ReadFromJsonAsync<T>();
        return Ok(new Response<T>(true, data!));
    }
}