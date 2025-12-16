namespace DF.TrackingService.Contracts.Models.Responses;

public record BusinessLocationResponse(
    Guid Id,
    Guid LocationId,
    LocationResponse Location,
    Guid BusinessId
    );