using DF.Contracts.Gateway.Requests.Auth;
using DF.Contracts.Gateway.Responses;
using DF.UserService.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DF.UserService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService,
    ILogger<AuthController> logger
    ) : ControllerBase
{
    private const string RefreshTokenCookieName = "refreshToken";

    [HttpPost("register")]

    public async Task<ActionResult<TokenResponse>> Register([FromBody] RegisterRequest request)
    {
        logger.LogInformation("REGISTER START {@Request}", request);

        try
        {
            var tokens = await authService.RegisterAsync(request);

            if (tokens == null)
            {
                logger.LogWarning("REGISTER FAILED: tokens is null");
                return BadRequest("Registration failed");
            }

            SetRefreshTokenCookie(tokens.RefreshToken, tokens.AccessTokenExpiresAt);

            logger.LogInformation("REGISTER SUCCESS for {Email}", request.Email);

            return Ok(new TokenResponse(
                tokens.AccessToken,
                tokens.RefreshToken,
                tokens.AccessTokenExpiresAt
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "REGISTER EXCEPTION for {Email}", request.Email);

            return BadRequest(new { error = ex.Message });
        }
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