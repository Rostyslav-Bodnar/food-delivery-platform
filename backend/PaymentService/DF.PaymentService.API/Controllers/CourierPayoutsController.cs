using DF.PaymentService.Application.Services.Interfaces;
using DF.PaymentService.Contracts.Payments;
using Microsoft.AspNetCore.Mvc;

namespace DF.PaymentService.API.Controllers;

[ApiController]
[Route("api/courier-payouts")]
public class CourierPayoutsController(ICourierPayoutService courierPayoutService) : ControllerBase
{
    [HttpGet("{courierId:guid}/balance")]
    [ProducesResponseType(typeof(CourierBalanceResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalance([FromRoute] Guid courierId, CancellationToken ct)
    {
        var snapshot = await courierPayoutService.GetBalanceAsync(courierId, ct);
        return Ok(new CourierBalanceResponseDto
        {
            CourierId = snapshot.CourierId,
            PendingAmount = snapshot.PendingAmount,
            AvailableAmount = snapshot.AvailableAmount,
            Currency = snapshot.Currency,
            StripeAccountId = snapshot.StripeAccountId,
            PayoutsEnabled = snapshot.PayoutsEnabled
        });
    }

    [HttpPut("{courierId:guid}/account")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertAccount(
        [FromRoute] Guid courierId,
        [FromBody] UpdateCourierPayoutAccountRequest request,
        CancellationToken ct)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.StripeAccountId))
            return BadRequest("StripeAccountId is required.");

        await courierPayoutService.UpsertCourierStripeAccountAsync(courierId, request.StripeAccountId, ct);
        return NoContent();
    }
}
