using DF.Contracts.Gateway.Requests.Auth;
using DF.Contracts.Gateway.Responses;
using DF.UserService.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DF.UserService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IAuthService authService,
    IConfiguration configuration,
    ILogger<AuthController> logger
    ) : ControllerBase
{
    private const string RefreshTokenCookieName = "refreshToken";

    [HttpPost("register")]
    public async Task<ActionResult<TokenResponse>> Register([FromBody] RegisterRequest request)
    {
        logger.LogInformation("REGISTER START for {Email}", request.Email);

        var tokens = await authService.RegisterAsync(request);

        if (tokens == null)
        {
            logger.LogWarning("REGISTER FAILED: tokens is null");
            return BadRequest("Registration failed");
        }

        SetRefreshTokenCookie(tokens.RefreshToken);

        logger.LogInformation("REGISTER SUCCESS for {Email}", request.Email);

        return Ok(tokens);
    }


    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest request)
    {
        var tokens = await authService.LoginAsync(request);

        SetRefreshTokenCookie(tokens.RefreshToken);

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

        var tokens = await authService.RefreshAsync(refreshToken);

        if (tokens is null)
        {
            Response.Cookies.Delete(RefreshTokenCookieName);
            throw new UnauthorizedAccessException("Invalid or expired refresh token");
        }

        SetRefreshTokenCookie(tokens.RefreshToken);

        return Ok(tokens);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
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

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var refreshDays = configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays");

        Response.Cookies.Append(
            RefreshTokenCookieName,
            refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(refreshDays)
            }
        );
    }
}