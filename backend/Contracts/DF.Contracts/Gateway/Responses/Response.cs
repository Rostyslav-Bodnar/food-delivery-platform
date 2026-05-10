namespace DF.Contracts.Gateway.Responses;

public record Response<T>(
    bool Success,
    T Data,
    string? ErrorMassage = null
    );