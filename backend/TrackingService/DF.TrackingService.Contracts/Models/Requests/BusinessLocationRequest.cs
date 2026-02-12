namespace DF.TrackingService.Contracts.Models.Requests;

public record CreateBusinessLocationRequest(
    Guid BusinessId,
    Guid LocationId
    );