using DF.UserService.Contracts.Models.Response;
using DF.UserService.Domain.Entities;

namespace DF.UserService.Application.Services.Interfaces;

public interface ITokenService
{
    Task<TokenResponse> GenerateTokensAsync(User user);
    Task<TokenResponse?> RefreshAsync(string refreshToken);
    Task RevokeRefreshTokenAsync(string refreshToken);
}