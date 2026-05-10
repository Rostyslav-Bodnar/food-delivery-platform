using DF.Contracts.Gateway.Requests.Auth;
using DF.Contracts.Gateway.Responses;
using DF.UserService.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DF.UserService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private const string RefreshTokenCookieName = "refreshToken";

    [HttpPost("register")]
    public async Task<ActionResult<TokenResponse>> Register(
        [FromBody] RegisterRequest request)
    {
        try
        {
            var tokens = await authService.RegisterAsync(request);

            SetRefreshTokenCookie(tokens.RefreshToken, tokens.AccessTokenExpiresAt);
            return Ok(new TokenResponse(
                tokens.AccessToken,
                tokens.RefreshToken,
                tokens.AccessTokenExpiresAt
            ));
        }
        catch (Exception exception)
        {
            return NotFound(new ServiceErrorResponse(
                Code: "INVALID_DATA",
                Message: exception.Message
            ));
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login(
        [FromBody] LoginRequest request)
    {
        try
        {
            var tokens = await authService.LoginAsync(request);

            SetRefreshTokenCookie(tokens.RefreshToken, tokens.AccessTokenExpiresAt);
            return Ok(new TokenResponse(
                tokens.AccessToken,
                tokens.RefreshToken,
                tokens.AccessTokenExpiresAt
            ));
        }
        catch (Exception exception)
        {
            return NotFound(new ServiceErrorResponse(
                Code: "INVALID_DATA",
                Message: exception.Message
            ));
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponse>> Refresh()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];

        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new ServiceErrorResponse(
                Code: "NO_REFRESH_TOKEN",
                Message: "Refresh token is missing"
            ));
        }

        var tokens = await authService.RefreshAsync(refreshToken);

        if (tokens is null)
        {
            return Unauthorized(new ServiceErrorResponse(
                Code: "INVALID_REFRESH_TOKEN",
                Message: "Invalid or expired refresh token"
            ));
        }

        SetRefreshTokenCookie(tokens.RefreshToken, tokens.AccessTokenExpiresAt);

        return Ok(new TokenResponse(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.AccessTokenExpiresAt
        ));
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];

        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest(new ServiceErrorResponse(
                Code: "NO_REFRESH_TOKEN",
                Message: "Refresh token is missing"
            ));
        }

        await authService.RevokeAsync(refreshToken);
        Response.Cookies.Delete(RefreshTokenCookieName);

        return NoContent();
    }

    private void SetRefreshTokenCookie(string refreshToken, DateTime expiresAt)
    {
        Response.Cookies.Append(
            RefreshTokenCookieName,
            refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // true in production
                SameSite = SameSiteMode.Lax,
                Expires = expiresAt
            }
        );
    }
}