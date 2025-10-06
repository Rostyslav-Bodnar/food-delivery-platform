using DF.UserService.Contracts.Models.Request;
using DF.UserService.Contracts.Models.Response;

namespace DF.UserService.Application.Interfaces;

public interface IAuthService
{
    Task<TokenResponse> RegisterAsync(RegisterRequest request);
    Task<TokenResponse> LoginAsync(LoginRequest request);
    Task<TokenResponse?> RefreshAsync(string refreshToken);
    Task RevokeAsync(string refreshToken);
}