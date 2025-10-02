namespace DF.UserService.Contracts.Models.Response;

public record TokenResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt);
