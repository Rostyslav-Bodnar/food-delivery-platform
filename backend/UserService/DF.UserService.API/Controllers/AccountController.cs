using DF.UserService.Application.Interfaces;
using DF.UserService.Contracts.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DF.UserService.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AccountController(IAccountService accountService) : ControllerBase
{
    /// <summary>
    /// Get account by userId
    /// </summary>
    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<AccountDTO>> GetAccount(Guid userId)
    {
        var account = await accountService.GetAccountByUserAsync(userId);

        if (account == null)
            return NotFound($"Account for user {userId} not found.");

        return Ok(account);
    }

    /// <summary>
    /// Get accounts by userId
    /// </summary>
    [HttpGet("all/{userId:guid}")]
    public async Task<ActionResult<IEnumerable<AccountDTO>>> GetAccounts(Guid userId)
    {
        var accounts = await accountService.GetAccountsByUserAsync(userId);

        return Ok(accounts);
    }

    /// <summary>
    /// Create account
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AccountDTO>> CreateAccount([FromBody] AccountDTO? account)
    {
        if (account == null)
            return BadRequest("Account data is required.");

        var created = await accountService.CreateAccountAsync(account);

        return CreatedAtAction(nameof(GetAccount), new { userId = created.UserId }, created);
    }

    /// <summary>
    /// Update account
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AccountDTO>> UpdateAccount(Guid id, [FromBody] AccountDTO? account)
    {
        if (account == null || id.ToString() != account.Id)
            return BadRequest("Invalid account data.");

        var updated = await accountService.UpdateAccountAsync(account);

        return Ok(updated);
    }

    /// <summary>
    /// Delete account
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAccount(Guid id)
    {
        var result = await accountService.DeleteAccountAsync(id);

        if (!result)
            return NotFound($"Account with id {id} not found.");

        return NoContent();
    }
}
