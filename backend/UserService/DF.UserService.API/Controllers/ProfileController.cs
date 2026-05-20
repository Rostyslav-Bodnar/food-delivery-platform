using DF.Contracts.Gateway.Responses;
using DF.UserService.API.Middlewares;
using DF.UserService.Application.Services.Interfaces;
using DF.UserService.Contracts.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace DF.UserService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfileController(
    IAccountService accountService,
    IUserService userService,
    IUserContext userContext,
    IConfiguration configuration,
    ITokenService tokenService) : ControllerBase
{
    private const string RefreshTokenCookieName = "refreshToken";
    
    [HttpGet]
    public async Task<ActionResult<ProfileResponse>> GetProfile()
    {
        var userId = userContext.UserId;
        if(userId == null)
            throw new UnauthorizedAccessException("User is not authenticated");

        var user = await userService.GetUserAsync(userId);

        var accounts = await accountService.GetAccountsByUserAsync(userId) ?? [];

        var currentAccount =
            accounts.FirstOrDefault(a => a.Id == user.CurrentAccount.Id)
            ?? accounts.FirstOrDefault();

        return Ok(new ProfileResponse(user, currentAccount, accounts));
    }

    [HttpPut("switch/{accountId:guid}")]
    public async Task<ActionResult<TokenResponse>> SwitchAccount(Guid accountId)
    {
        var userId = userContext.UserId;
        if(userId == Guid.Empty)
            throw new UnauthorizedAccessException("User is not authenticated");

        var user = await userService.GetUserEntityAsync(userId)
                   ?? throw new NotFoundException("User not found");

        var accounts = await accountService.GetAccountsByUserAsync(userId);

        if (accounts == null || !accounts.Any(a => a.Id == accountId.ToString()))
            throw new NotFoundException("Account not found or not owned by user");

        user.AccountId = accountId;

        await userService.UpdateUserAsync(user);

        // 🔥 generate NEW JWT with new account_id/account_type
        var tokens = await tokenService.GenerateTokensAsync(user);

        SetRefreshTokenCookie(tokens.RefreshToken);

        return Ok(tokens);
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
