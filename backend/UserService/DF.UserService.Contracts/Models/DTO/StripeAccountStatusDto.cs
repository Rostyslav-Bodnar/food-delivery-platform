namespace DF.UserService.Contracts.Models.DTO;

public record StripeAccountStatusDto(
    bool ChargesEnabled,
    bool PayoutsEnabled,
    string RequirementsDue
);