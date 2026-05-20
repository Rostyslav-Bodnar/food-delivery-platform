namespace DF.TrackingService.Application.Services.Interfaces;

public interface IUserContext
{
    Guid UserId { get; }

    /// <summary>The caller's active Account.Id, sourced from the JWT account_id claim. Guid.Empty if absent.</summary>
    Guid AccountId { get; }

    /// <summary>The caller's active account type, sourced from account_type ("Customer" / "Business" / "Courier"). Null if absent.</summary>
    string? AccountType { get; }

    bool IsBusiness { get; }
}
