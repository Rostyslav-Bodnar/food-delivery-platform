using DF.UserService.Application.Interfaces;
using DF.UserService.Contracts.Models.Request;
using DF.UserService.Contracts.Models.Response;
using DF.UserService.Domain.Entities;
using DF.UserService.Infrastructure.Messaging;
using Microsoft.AspNetCore.Identity;

namespace DF.UserService.Application.Services;

public class AuthService(UserManager<User> userManager, ITokenService tokenService, IMessageBroker broker)
    : IAuthService
{
    public async Task<TokenResponse> RegisterAsync(RegisterRequest request)
    {
        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

        // Publish event (наприклад для нотифікацій або email service)
        broker.Publish("user.registered", new
        {
            user.Id,
            user.Email,
            user.FullName,
            user.CreatedAt
        });

        return await tokenService.GenerateTokensAsync(user);
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
            throw new Exception("User not found");

        var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
            throw new Exception("Invalid password");

        return await tokenService.GenerateTokensAsync(user);
    }

    public async Task<TokenResponse?> RefreshAsync(string refreshToken)
    {
        return await tokenService.RefreshAsync(refreshToken);
    }

    public async Task RevokeAsync(string refreshToken)
    {
        await tokenService.RevokeRefreshTokenAsync(refreshToken);
    }
}