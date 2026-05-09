namespace DF.MenuService.Contracts.Exceptions;

public class StripeAccountNotReadyException : Exception
{
    public StripeAccountNotReadyException()
        : base("Stripe account is not fully onboarded or not enabled.")
    {
    }

    public StripeAccountNotReadyException(string message)
        : base(message)
    {
    }
}