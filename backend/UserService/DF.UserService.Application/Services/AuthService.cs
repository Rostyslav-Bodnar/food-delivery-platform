using DF.UserService.Application.Services.Interfaces;
using DF.UserService.Contracts.Models.DTO;
using DF.UserService.Contracts.Models.Request;
using DF.UserService.Contracts.Models.Response;
using DF.UserService.Domain.Entities;
using DF.UserService.Infrastructure.Messaging;
using Microsoft.AspNetCore.Identity;

namespace DF.UserService.Application.Services;

public class AuthService(
    UserManager<User> userManager, 
    ITokenService tokenService, 
    IMessageBroker broker,
    IAccountService accountService)
    : IAuthService
{
    public async Task<TokenResponse> RegisterAsync(RegisterRequest request)
    {
        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            Name = request.Name,
            Surname = request.Surname,
            UserRole = UserRole.User
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

        var customerDto = new CreateCustomerAccountRequest(
            AccountType: 0,
            ImageFile: null,
            PhoneNumber: null,
            Name: user.Name,
            Surname: user.Surname,
            Address: null
        );

        var accountDto = await accountService.CreateAccountAsync(customerDto, user.Id);

        user.AccountId = Guid.Parse(accountDto.Id);

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            throw new Exception(string.Join(", ", updateResult.Errors.Select(e => e.Description)));

        broker.Publish("user.registered", new
        {
            user.Id,
            user.Email,
            user.Name,
            user.Surname,
            user.CreatedAt,
            accountDto.AccountType
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

    public async Task<TokenResponse?> RefreshAsync(string refreshToken) =>
        await tokenService.RefreshAsync(refreshToken);

    public async Task RevokeAsync(string refreshToken) =>
        await tokenService.RevokeRefreshTokenAsync(refreshToken);
}