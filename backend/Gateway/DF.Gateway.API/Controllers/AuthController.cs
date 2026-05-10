using System.Security.Claims;
using DF.Contracts.Gateway.Requests.Auth;
using DF.Contracts.Gateway.Responses;
using DF.Gateway.API.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace DF.Gateway.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    InternalHttpClient http) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<Response<TokenResponse>>> Register(
        [FromBody] RegisterRequest request)
        => await Forward<TokenResponse>("/api/auth/register", request);

    [HttpPost("login")]
    public async Task<ActionResult<Response<TokenResponse>>> Login(
        [FromBody] LoginRequest request)
        => await Forward<TokenResponse>("/api/auth/login", request);

    [HttpPost("refresh")]
    public async Task<ActionResult<Response<TokenResponse>>> Refresh()
        => await Forward<TokenResponse>("/api/auth/refresh");

    [HttpPost("logout")]
    public async Task<ActionResult<Response<bool>>> Logout()
        => await Forward<bool>("/api/auth/revoke");

    private async Task<ActionResult<Response<T>>> Forward<T>(
        string url,
        object? body = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);

        if (body is not null)
            request.Content = JsonContent.Create(body);

        CopyCookies(request);

        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";

        var response = await http.SendAsync(request, userId);

        CopySetCookies(response);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content
                .ReadFromJsonAsync<ServiceErrorResponse>();

            return Ok(new Response<T>(
                false,
                default,
                error?.Message ?? "Auth error"
            ));
        }

        var data = await response.Content.ReadFromJsonAsync<T>();
        return Ok(new Response<T>(true, data!));
    }

    private void CopyCookies(HttpRequestMessage request)
    {
        if (Request.Headers.TryGetValue("Cookie", out var cookies))
            request.Headers.Add("Cookie", cookies.ToArray());
    }

    private void CopySetCookies(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            foreach (var c in cookies)
                Response.Headers.Append("Set-Cookie", c);
        }
    }
}