namespace DF.PaymentService.Domain.Entities;

public enum CourierEarningStatus
{
    Blocked = 0,
    Pending = 1,
    Processing = 2,
    Paid = 3,
    Voided = 4
}
