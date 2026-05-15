

using DF.Contracts.Gateway.Requests.Auth;
using DF.Contracts.Gateway.Responses;

namespace DF.UserService.Application.Services.Interfaces;

public interface IAuthService
{
    Task<TokenResponse> RegisterAsync(RegisterRequest request);
    Task<TokenResponse> LoginAsync(LoginRequest request);
    Task<TokenResponse?> RefreshAsync(string refreshToken);
    Task RevokeAsync(string refreshToken);
}