namespace DF.Contracts.Gateway.Requests.Auth;

public record RegisterRequest(string Email, string Password, string Name, string Surname);
public record LoginRequest(string Email, string Password);