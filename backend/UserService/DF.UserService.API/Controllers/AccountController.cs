using DF.Contracts.Gateway.Requests.Accounts;
using DF.Contracts.Gateway.Responses;
using DF.UserService.API.Middlewares;
using DF.UserService.Application.Services.Interfaces;
using DF.UserService.Contracts.Exceptions;
using DF.UserService.Contracts.Models.Response;
using Microsoft.AspNetCore.Mvc;

namespace DF.UserService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController(
    IAccountService accountService,
    IUserContext userContext,
    IBusinessDashboardService dashboardService) : ControllerBase
{
    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<AccountResponse>> GetAccount(Guid userId)
    {
        var account = await accountService.GetAccountByUserAsync(userId)
                      ?? throw new NotFoundException("Account not found");

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
            throw new NotFoundException("No business accounts found");

        return Ok(result);
    }

    [HttpPost("courier")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<AccountResponse>> CreateCourierAccount(
        [FromForm] CreateCourierAccountRequest request)
        => await CreateAccount(request);

    [HttpPost("customer")]
    public async Task<ActionResult<AccountResponse>> CreateCustomerAccount(
        [FromForm] CreateCustomerAccountRequest request)
        => await CreateAccount(request);

    [HttpPost("business")] 
    [Consumes("multipart/form-data")]
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
            throw new NotFoundException("Account not found");

        return NoContent();
    }

    [HttpGet("onboarding/{businessId:guid}")]
    public async Task<ActionResult<OnboardingLinkResponse>> GetOnboardingLink(Guid businessId)
    {
        var result = await accountService.GetOnboardingLinkAsync(
            businessId,
            HttpContext.RequestAborted);

        if (result.Status == OnboardingLinkResponse.ProvisioningStatus)
        {
            // 202 Accepted: account exists but Stripe provisioning is still
            // in flight (StripeAccountProvisioningWorker). Client should retry.
            if (result.RetryAfterSeconds is int retry)
            {
                Response.Headers["Retry-After"] = retry.ToString();
            }
            return StatusCode(StatusCodes.Status202Accepted, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Live financial dashboard for a business: KPI summary, daily income
    /// series, outcome breakdown, payout history. Data is read fresh from
    /// the business's Stripe Connect account on every call (no local
    /// aggregation table yet).
    /// </summary>
    [HttpGet("business/{businessId:guid}/dashboard")]
    public async Task<ActionResult<BusinessDashboardResponse>> GetBusinessDashboard(
        Guid businessId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var toUtc = (to ?? DateTime.UtcNow).ToUniversalTime();
        var fromUtc = (from ?? toUtc.AddDays(-30)).ToUniversalTime();

        var dashboard = await dashboardService.GetDashboardAsync(
            businessId,
            fromUtc,
            toUtc,
            HttpContext.RequestAborted);

        if (dashboard is null)
            throw new NotFoundException(
                "Business not found or Stripe onboarding has not completed yet.");

        return Ok(dashboard);
    }

    /// <summary>
    /// Drain the connected account's available balance to the business's
    /// bank account on demand. Normal Express accounts pay out
    /// automatically on Stripe's schedule — this is the manual override.
    /// Fires `payout.created` (→ `payout.paid` / `payout.failed`) which
    /// hits StripePayoutWebhookController and lands as a PayoutRecord.
    /// </summary>
    [HttpPost("business/{businessId:guid}/payouts/manual")]
    public async Task<ActionResult<ManualPayoutResponse>> CreateManualPayout(Guid businessId)
    {
        var userId = userContext.UserId;
        if (userId == Guid.Empty)
            throw new UnauthorizedAccessException("User is not authorized");

        var result = await dashboardService.CreateManualPayoutAsync(
            businessId,
            userId,
            HttpContext.RequestAborted);

        return Ok(result);
    }
}