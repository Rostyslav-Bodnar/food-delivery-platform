using DF.UserService.Application.Interfaces;
using DF.UserService.Application.Services;
using DF.UserService.Contracts.Models.Request;
using DF.UserService.Contracts.Models.Response;
using DF.UserService.Domain.Entities;
using DF.UserService.Infrastructure.Messaging;
using Microsoft.AspNetCore.Identity;

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
            FullName = request.FullName,
            UserRole = UserRole.User
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

        // Створюємо акаунт користувача
        var accountDto = await accountService.CreateAccountAsync(user.Id, AccountType.Customer, null);

        // Прив'язуємо акаунт до користувача
        user.AccountId = Guid.Parse(accountDto.Id);
        user.CurrentAccount = new Account
        {
            Id = user.AccountId,
            UserId = user.Id,
            AccountType = AccountType.Customer
        };

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            throw new Exception(string.Join(", ", updateResult.Errors.Select(e => e.Description)));

        // Публікація повідомлення про нову реєстрацію
        broker.Publish("user.registered", new
        {
            user.Id,
            user.Email,
            user.FullName,
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
