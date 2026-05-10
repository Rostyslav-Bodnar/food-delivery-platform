using DF.Contracts.Gateway.Responses;
using DF.UserService.API.Middlewares;
using DF.UserService.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DF.UserService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfileController(
    IAccountService accountService,
    IUserService userService,
    IUserContext userContext) : ControllerBase
{
    // =========================
    // GET PROFILE
    // =========================
    [HttpGet]
    public async Task<ActionResult<ProfileResponse>> GetProfile()
    {
        var userId = userContext.UserId;
        if (userId == null)
        {
            return Unauthorized(new ServiceErrorResponse(
                Code: "UNAUTHORIZED",
                Message: "User is not authenticated"
            ));
        }

        var user = await userService.GetUserAsync(userId);
        if (user == null)
        {
            return NotFound(new ServiceErrorResponse(
                Code: "USER_NOT_FOUND",
                Message: "User not found"
            ));
        }

        var accounts = await accountService.GetAccountsByUserAsync(userId) ?? [];

        var currentAccount =
            accounts.FirstOrDefault(a => a.Id == user.CurrentAccount.Id)
            ?? accounts.FirstOrDefault();

        var profile = new ProfileResponse(user, currentAccount, accounts);
        return Ok(profile);
    }

    // =========================
    // SWITCH ACCOUNT
    // =========================
    [HttpPut("switch/{accountId:guid}")]
    public async Task<IActionResult> SwitchAccount(Guid accountId)
    {
        var userId = userContext.UserId;
        if (userId == null)
        {
            return Unauthorized(new ServiceErrorResponse(
                Code: "UNAUTHORIZED",
                Message: "User is not authenticated"
            ));
        }

        var user = await userService.GetUserEntityAsync(userId);
        if (user == null)
        {
            return NotFound(new ServiceErrorResponse(
                Code: "USER_NOT_FOUND",
                Message: "User not found"
            ));
        }

        var accounts = await accountService.GetAccountsByUserAsync(userId);
        if (accounts == null || !accounts.Any(a => a.Id == accountId.ToString()))
        {
            return NotFound(new ServiceErrorResponse(
                Code: "ACCOUNT_NOT_FOUND",
                Message: "Account not found or not owned by user"
            ));
        }

        user.AccountId = accountId;
        await userService.UpdateUserAsync(user);

        return Ok(new
        {
            message = "Active account switched successfully",
            newAccountId = accountId
        });
    }
}
