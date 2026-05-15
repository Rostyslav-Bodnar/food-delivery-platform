namespace DF.PaymentService.Domain.Entities.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; }
}