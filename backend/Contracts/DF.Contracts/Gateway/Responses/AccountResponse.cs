using System;
using System.Text.Json.Serialization;

namespace DF.Contracts.Gateway.Responses;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(CustomerAccountResponse), "customer")]
[JsonDerivedType(typeof(BusinessAccountResponse), "business")]
[JsonDerivedType(typeof(CourierAccountResponse), "courier")]
public abstract record AccountResponse
{
    public string? Id { get; init; }
    public string? UserId { get; init; }
    public string AccountType { get; init; } = default!;
    public string? ImageUrl { get; init; }
}


public record CustomerAccountResponse : AccountResponse
{
    public string? PhoneNumber { get; init; }
    public string? Name { get; init; }
    public string? Surname { get; init; }
    public string? Address { get; init; }
}


public record BusinessAccountResponse : AccountResponse
{
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public string? StripeAccountId { get; init; }
    public bool? StripeChargesEnabled { get; init; }
    public bool? StripePayoutsEnabled { get; init; }
    public string? StripeRequirementsDue { get; init; }
    public DateTime? StripeOnboardedAt { get; init; }
}


public record CourierAccountResponse : AccountResponse
{
    public string? PhoneNumber { get; init; }
    public string? Name { get; init; }
    public string? Surname { get; init; }
    public string? Address { get; init; }
    public string? Description { get; init; }
}



