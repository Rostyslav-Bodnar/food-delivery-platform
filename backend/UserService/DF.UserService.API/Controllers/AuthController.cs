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
    public async Task<ActionResult<TokenResponse>> Register([FromBody] RegisterRequest request)
    {
        var tokens = await authService.RegisterAsync(request);

        SetRefreshTokenCookie(tokens.RefreshToken, tokens.AccessTokenExpiresAt);

        return Ok(new TokenResponse(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.AccessTokenExpiresAt
        ));
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest request)
    {
        var tokens = await authService.LoginAsync(request);

        SetRefreshTokenCookie(tokens.RefreshToken, tokens.AccessTokenExpiresAt);

        return Ok(new TokenResponse(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.AccessTokenExpiresAt
        ));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponse>> Refresh()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName]
                           ?? throw new UnauthorizedAccessException("Refresh token is missing");

        var tokens = await authService.RefreshAsync(refreshToken)
                     ?? throw new UnauthorizedAccessException("Invalid or expired refresh token");

        SetRefreshTokenCookie(tokens.RefreshToken, tokens.AccessTokenExpiresAt);

        return Ok(tokens);
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];

        if (string.IsNullOrEmpty(refreshToken))
        {
            throw new BadHttpRequestException("Refresh token is missing");
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
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = expiresAt
            }
        );
    }
}