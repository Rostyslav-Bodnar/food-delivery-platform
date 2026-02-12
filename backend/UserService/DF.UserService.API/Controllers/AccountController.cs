using System.Security.Claims;
using DF.UserService.Application.Services.Interfaces;
using DF.UserService.Contracts.Models.DTO;
using DF.UserService.Contracts.Models.Request;
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
    public async Task<ActionResult<AccountResponse>> GetAccount(Guid userId)
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
    public async Task<ActionResult<IEnumerable<AccountResponse>>> GetAccounts(Guid userId)
    {
        var accounts = await accountService.GetAccountsByUserAsync(userId);

        return Ok(accounts);
    }

    [HttpGet("all/business")]
    public async Task<IActionResult> GetAllBusinessAccounts()
    {
        var result = await accountService.GetBusinessAccountsAsync();
        
        if(result == null)
            return NotFound($"Business accounts for user {User.Identity.Name} not found.");
        
        return Ok(result);
    }

    [HttpPost("courier")]
    public async Task<ActionResult<AccountResponse>> CreateCourierAccount([FromForm] CreateCourierAccountRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized("User ID not found in token");
        
        var created = await accountService.CreateAccountAsync(request, Guid.Parse(userIdClaim.Value));
        return CreatedAtAction(nameof(GetAccount), new { userId = created.UserId }, created);
    }

    [HttpPost("customer")]
    public async Task<ActionResult<AccountResponse>> CreateCustomerAccount([FromForm] CreateCustomerAccountRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized("User ID not found in token");


        var created = await accountService.CreateAccountAsync(request, Guid.Parse(userIdClaim.Value));
        return CreatedAtAction(nameof(GetAccount), new { userId = created.UserId }, created);
    }

    [HttpPost("business")]
    public async Task<ActionResult<AccountResponse>> CreateBusinessAccount([FromForm] CreateBusinessAccountRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized("User ID not found in token");


        var created = await accountService.CreateAccountAsync(request, Guid.Parse(userIdClaim.Value));
        return CreatedAtAction(nameof(GetAccount), new { userId = created.UserId }, created);
    }


    
    [HttpPut("customer")]
    public async Task<ActionResult<AccountResponse>> UpdateCustomer([FromForm] UpdateCustomerAccountRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        request = request with { UserId = userId };
        var updated = await accountService.UpdateAccountAsync(request);
        return Ok(updated);
    }

    [HttpPut("business")]
    public async Task<ActionResult<AccountResponse>> UpdateBusiness([FromForm] UpdateBusinessAccountRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        request = request with { UserId = userId };
        var updated = await accountService.UpdateAccountAsync(request);
        return Ok(updated);
    }
    
    [HttpPut("courier")]
    public async Task<ActionResult<AccountResponse>> UpdateCourier([FromForm] UpdateCourierAccountRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        request = request with { UserId = userId };
        var updated = await accountService.UpdateAccountAsync(request);
        return Ok(updated);
    }
    
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAccount(Guid id)
    {
        var result = await accountService.DeleteAccountAsync(id);

        if (!result)
            return NotFound($"Account with id {id} not found.");

        return NoContent();
    }

}
