namespace DF.UserService.Domain.Entities;

public class BusinessAccount : Account
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? StripeAccountId { get; set; }
    public bool? StripeChargesEnabled { get; set; }
    public bool? StripePayoutsEnabled { get; set; }
    public string? StripeRequirementsDue { get; set; }
    public DateTime? StripeOnboardedAt  { get; set; }
}