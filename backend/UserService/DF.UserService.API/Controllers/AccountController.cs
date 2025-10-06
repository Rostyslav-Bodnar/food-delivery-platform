using System.Security.Claims;
using System.Text.Json;
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
    public async Task<ActionResult<AccountDTO>> CreateAccount([FromBody] JsonElement json)
    {
        // Отримуємо тип акаунту
        if (!json.TryGetProperty("accountType", out var accountTypeProp))
            return BadRequest("Account type is required.");

        var accountType = accountTypeProp.GetString();
        if (string.IsNullOrWhiteSpace(accountType))
            return BadRequest("Invalid account type.");

        // Отримуємо userId з токена
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized("User ID not found in token");

        var userId = userIdClaim.Value;

        var account = DeserializeAccount(json, accountType, userId);

        if (account == null)
            return BadRequest($"Unknown account type: {accountType}");

        // Створюємо акаунт
        var created = await accountService.CreateAccountAsync(account);

        return CreatedAtAction(nameof(GetAccount), new { userId = created.UserId }, created);
    }
    
    /// <summary>
    /// Update account
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<AccountDTO>> UpdateAccount([FromBody] JsonElement json)
    {
        if (!json.TryGetProperty("accountType", out var accountTypeProp))
            return BadRequest("Account type is required.");

        var accountType = accountTypeProp.GetString();
        if (string.IsNullOrWhiteSpace(accountType))
            return BadRequest("Invalid account type.");

        var account = DeserializeAccount(json, accountType, userId: null);
        if (account == null)
            return BadRequest($"Unknown account type: {accountType}");
        
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized("Invalid token or user id");

        account = account with { UserId = userId.ToString() };

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
    
    private static AccountDTO? DeserializeAccount(JsonElement json, string accountType, string userId)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        return accountType switch
        {
            "Customer" => JsonSerializer.Deserialize<CustomerAccountDTO>(json, options) with { UserId = userId },
            "Business" => JsonSerializer.Deserialize<BusinessAccountDTO>(json, options) with { UserId = userId },
            "Courier"  => JsonSerializer.Deserialize<CourierAccountDTO>(json, options) with { UserId = userId },
            _ => null
        };
    }

}
