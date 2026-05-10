using System;

namespace DF.Contracts.Gateway.Responses;

public record TokenResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt);
