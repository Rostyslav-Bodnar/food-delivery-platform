using System.Security.Claims;
using DF.UserService.Application.Services.Interfaces;
using DF.UserService.Contracts.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DF.UserService.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController(IAccountService accountService, IUserService userService) : ControllerBase
    {

        /// <summary>
        /// Get full profile info (user + active account + all accounts)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ProfileDTO>> GetProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized("Invalid token or user id");

            var user = await userService.GetUserAsync(userId);
            var accounts = await accountService.GetAccountsByUserAsync(userId) ?? [];

            var currentAccount = accounts.FirstOrDefault(a => a.Id == user.CurrentAccount.Id)
                                 ?? accounts.FirstOrDefault();

            var profile = new
            {
                user,
                currentAccount,
                accounts
            };
            return Ok(profile);
        }

        /// <summary>
        /// Switch active account
        /// </summary>
        [HttpPut("switch/{accountId:guid}")]
        public async Task<IActionResult> SwitchAccount(Guid accountId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized("Invalid token or user id");

            var user = await userService.GetUserEntityAsync(userId);
            if (user == null)
                return NotFound("User not found");

            var accounts = await accountService.GetAccountsByUserAsync(userId);
            if (accounts == null || !accounts.Any(a => a.Id == accountId.ToString()))
                return NotFound("Account not found or not owned by user");

            user.AccountId = accountId;
            await userService.UpdateUserAsync(user);

            return Ok(new { message = "Active account switched successfully", newAccountId = accountId });
        }
        
    }
}
