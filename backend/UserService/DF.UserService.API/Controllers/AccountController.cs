using DF.Contracts.Gateway.Requests.Accounts;
using DF.Contracts.Gateway.Responses;
using DF.UserService.API.Middlewares;
using DF.UserService.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DF.UserService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController(IAccountService accountService, IUserContext userContext) : ControllerBase
{
    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<AccountResponse>> GetAccount(Guid userId)
    {
        var account = await accountService.GetAccountByUserAsync(userId)
                      ?? throw new NullReferenceException("Account not found");

        return Ok(account);
    }

    [HttpGet("all/{userId:guid}")]
    public async Task<ActionResult<IEnumerable<AccountResponse>>> GetAccounts(Guid userId)
    {
        var accounts = await accountService.GetAccountsByUserAsync(userId);
        return Ok(accounts);
    }

    [HttpGet("all/business")]
    public async Task<ActionResult<IEnumerable<AccountResponse>>> GetAllBusinessAccounts()
    {
        var result = await accountService.GetBusinessAccountsAsync();

        if (result == null || !result.Any())
            throw new NullReferenceException("No business accounts found");

        return Ok(result);
    }

    [HttpPost("courier")]
    public async Task<ActionResult<AccountResponse>> CreateCourierAccount(
        [FromForm] CreateCourierAccountRequest request)
        => await CreateAccount(request);

    [HttpPost("customer")]
    public async Task<ActionResult<AccountResponse>> CreateCustomerAccount(
        [FromForm] CreateCustomerAccountRequest request)
        => await CreateAccount(request);

    [HttpPost("business")]
    public async Task<ActionResult<AccountResponse>> CreateBusinessAccount(
        [FromForm] CreateBusinessAccountRequest request)
        => await CreateAccount(request);

    private async Task<ActionResult<AccountResponse>> CreateAccount(CreateAccountRequest request)
    {
        var userId = userContext.UserId;

        if (userId == Guid.Empty)
            throw new UnauthorizedAccessException("User is not authorized");

        var created = await accountService.CreateAccountAsync(request, userId);
        return Ok(created);
    }

    [HttpPut("customer")]
    public async Task<ActionResult<AccountResponse>> UpdateCustomer(
        [FromForm] UpdateCustomerAccountRequest request)
        => await UpdateAccount(request);

    [HttpPut("business")]
    public async Task<ActionResult<AccountResponse>> UpdateBusiness(
        [FromForm] UpdateBusinessAccountRequest request)
        => await UpdateAccount(request);

    [HttpPut("courier")]
    public async Task<ActionResult<AccountResponse>> UpdateCourier(
        [FromForm] UpdateCourierAccountRequest request)
        => await UpdateAccount(request);

    private async Task<ActionResult<AccountResponse>> UpdateAccount(UpdateAccountRequest request)
    {
        var userId = userContext.UserId;

        if (userId == Guid.Empty)
            throw new UnauthorizedAccessException("User is not authorized");

        request = request with { UserId = userId.ToString() };

        var updated = await accountService.UpdateAccountAsync(request);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAccount(Guid id)
    {
        var deleted = await accountService.DeleteAccountAsync(id);

        if (!deleted)
            throw new NullReferenceException("Account not found");

        return NoContent();
    }

    [HttpGet("onboarding/{businessId:guid}")]
    public async Task<ActionResult<string>> GetOnboardingLink(Guid businessId)
    {
        var link = await accountService.GetOnboardingLinkAsync(
            businessId,
            CancellationToken.None);

        if (string.IsNullOrEmpty(link))
            throw new NullReferenceException("Onboarding link not found");

        return Ok(link);
    }
}