namespace DF.OrderService.Contracts.Exceptions;

public class OrderStateException : Exception
{
    public OrderStateException(string message) : base(message) { }
}

public class IllegalStatusTransitionException(string current, string requested)
    : OrderStateException($"Cannot transition order from '{current}' to '{requested}'");
