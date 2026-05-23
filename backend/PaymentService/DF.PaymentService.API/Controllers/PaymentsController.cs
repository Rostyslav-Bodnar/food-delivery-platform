using DF.PaymentService.Application.CommandHandlers;
using DF.PaymentService.Application.Commands;
using DF.PaymentService.Application.Repositories.Interfaces;
using DF.PaymentService.Contracts.Payments;
using Microsoft.AspNetCore.Mvc;

namespace DF.PaymentService.API.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController(
    IPaymentRepository payments,
    CancelPaymentCommandHandler cancelHandler,
    RefundPaymentCommandHandler refundHandler,
    CollectCashCommandHandler collectHandler)
    : ControllerBase
{
    /// <summary>
    /// Отримати платіж за OrderId (для фронту: статус, method, client_secret)
    /// </summary>
    [HttpGet("{orderId:guid}")]
    [ProducesResponseType(typeof(PaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByOrderId([FromRoute] Guid orderId, CancellationToken ct)
    {
        var payment = await payments.GetByOrderIdAsync(orderId, ct);
        if (payment is null) return NotFound();

        var dto = PaymentResponseDto.FromDomain(payment);
        return Ok(dto);
    }

    /// <summary>
    /// Скасувати платіж (Online — скасовує PaymentIntent; CoD — скасовує очікування інкасації)
    /// </summary>
    [HttpPost("{paymentId:guid}/cancel")]
    // [Authorize(Policy = Policies.PaymentsManage)] // розкоментуйте у проді
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel([FromRoute] Guid paymentId, [FromBody] CancelPaymentRequest body, CancellationToken ct)
    {
        var payment = await payments.GetByIdAsync(paymentId, ct);
        if (payment is null) return NotFound();

        // Валідація стану тут додаткова — основна у домені/хендлері
        await cancelHandler.Handle(new CancelPaymentCommand(paymentId, body?.Reason), ct);
        return NoContent();
    }

    /// <summary>
    /// Рефанд: якщо amount=null → повний; якщо >0 → частковий (у валюті платежу)
    /// </summary>
    [HttpPost("{paymentId:guid}/refunds")]
    // [Authorize(Policy = Policies.PaymentsManage)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Refund(
        [FromRoute] Guid paymentId,
        [FromBody] RefundRequest body,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        // Проста валідація запиту
        if (body is null) return BadRequest("Body is required.");
        if (body.Amount.HasValue && body.Amount.Value <= 0)
            return BadRequest("Amount must be > 0 for partial refund.");

        var payment = await payments.GetByIdAsync(paymentId, ct);
        if (payment is null) return NotFound();

        await refundHandler.Handle(
            new RefundPaymentCommand(paymentId, body.Amount, idempotencyKey),
            ct);

        // Для Online ми чекаємо Stripe webhook → 202 Accepted (обробка асинхронна)
        return Accepted();
    }

    /// <summary>
    /// CoD інкасація (кур’єр/OMS підтверджує отримання готівки)
    /// </summary>
    [HttpPost("{paymentId:guid}/cash/collect")]
    // [Authorize(Policy = Policies.PaymentsManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CollectCash([FromRoute] Guid paymentId, CancellationToken ct)
    {
        var payment = await payments.GetByIdAsync(paymentId, ct);
        if (payment is null) return NotFound();

        await collectHandler.Handle(new CollectCashCommand(paymentId), ct);
        return NoContent();
    }

    /// <summary>
    /// Історія платежу (простий варіант: лише список рефандів та основні поля)
    /// </summary>
    [HttpGet("{paymentId:guid}/history")]
    // [Authorize] // налаштуйте доступ під свого користувача/бекофіс
    [ProducesResponseType(typeof(PaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> History([FromRoute] Guid paymentId, CancellationToken ct)
    {
        var payment = await payments.GetByIdAsync(paymentId, ct);
        if (payment is null) return NotFound();

        // Базово повертаємо той самий DTO, який містить Refunds
        var dto = PaymentResponseDto.FromDomain(payment);
        return Ok(dto);
    }
}