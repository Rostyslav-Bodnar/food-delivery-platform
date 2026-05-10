namespace DF.Contracts.Gateway.Responses;

public record ServiceErrorResponse(
    string Code,
    string Message,
    string? TraceId
);
