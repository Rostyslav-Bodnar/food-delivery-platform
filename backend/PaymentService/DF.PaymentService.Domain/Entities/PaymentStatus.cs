namespace DF.PaymentService.Domain.Entities;

public enum PaymentStatus
{
    Pending,
    RequiresAction, 
    AwaitingCashCollection, // cash payment method
    Succeeded,
    Failed,
    Cancelled,
    Refunded //return money
}