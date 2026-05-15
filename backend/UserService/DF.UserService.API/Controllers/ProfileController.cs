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
    public async Task<IActionResult> SwitchAccount(Guid accountId)
    {
        var userId = userContext.UserId;
        if(userId == null)
            throw new UnauthorizedAccessException("User is not authenticated");

        var user = await userService.GetUserEntityAsync(userId)
                   ?? throw new NullReferenceException("User not found");

        var accounts = await accountService.GetAccountsByUserAsync(userId);

        if (accounts == null || !accounts.Any(a => a.Id == accountId.ToString()))
            throw new NullReferenceException("Account not found or not owned by user");

        user.AccountId = accountId;
        await userService.UpdateUserAsync(user);

        return Ok(new
        {
            message = "Active account switched successfully",
            newAccountId = accountId
        });
    }
}
