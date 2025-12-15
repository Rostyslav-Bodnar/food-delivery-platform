namespace DF.OrderService.Domain.Entities;

public enum OrderStatus
{
    Canceled = 0,
    Preparing = 1,
    Ready = 2,
    OutForDelivery = 3,
    Delivered = 4,
}