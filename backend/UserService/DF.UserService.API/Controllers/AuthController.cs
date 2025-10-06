using Microsoft.AspNetCore.Mvc;
using DF.UserService.Application.Interfaces;
using DF.UserService.Contracts.Models.Request;
using DF.UserService.Contracts.Models.Response;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private const string RefreshTokenCookieName = "refreshToken";

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var tokens = await authService.RegisterAsync(request);

        SetRefreshTokenCookie(tokens.RefreshToken, tokens.AccessTokenExpiresAt);

        return Ok(new TokenResponse(tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiresAt));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var tokens = await authService.LoginAsync(request);

        SetRefreshTokenCookie(tokens.RefreshToken, tokens.AccessTokenExpiresAt);

        return Ok(new TokenResponse(tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiresAt));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized("No refresh token");

        var tokens = await authService.RefreshAsync(refreshToken);
        if (tokens == null)
            return Unauthorized("Invalid or expired refresh token");

        SetRefreshTokenCookie(tokens.RefreshToken, tokens.AccessTokenExpiresAt);

        return Ok(new TokenResponse(tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiresAt));
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        if (string.IsNullOrEmpty(refreshToken))
            return BadRequest("No refresh token");

        await authService.RevokeAsync(refreshToken);

        Response.Cookies.Delete(RefreshTokenCookieName);

        return NoContent();
    }

    private void SetRefreshTokenCookie(string refreshToken, DateTime expiresAt)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = false, // у production включай завжди
            SameSite = SameSiteMode.Lax,
            Expires = expiresAt
        };

        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, cookieOptions);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        if (string.IsNullOrEmpty(refreshToken))
            return BadRequest("No refresh token");

        await authService.RevokeAsync(refreshToken);

        Response.Cookies.Delete(RefreshTokenCookieName);

        return NoContent();
    }

}

