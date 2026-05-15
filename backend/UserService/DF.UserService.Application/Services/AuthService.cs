using DF.Contracts.Gateway.Requests.Auth;
using DF.Contracts.Gateway.Responses;
using DF.UserService.Application.Services.Interfaces;
using DF.UserService.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using CreateCustomerAccountRequest = DF.Contracts.Gateway.Requests.Accounts.CreateCustomerAccountRequest;

namespace DF.UserService.Application.Services;


public class AuthService(
    UserManager<User> userManager, 
    ITokenService tokenService, 
    IAccountService accountService,
    ILogger<AuthService> logger) // ✅ додаємо logger
    : IAuthService
{
    public async Task<TokenResponse> RegisterAsync(RegisterRequest request)
    {
        logger.LogInformation("REGISTER START for {Email}", request.Email);

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
        {
            var errors = result.Errors.Select(e => e.Description).ToList();

            logger.LogWarning("USER CREATION FAILED for {Email}. Errors: {Errors}",
                request.Email,
                string.Join(", ", errors));

            throw new Exception(string.Join(", ", errors));
        }

        logger.LogInformation("USER CREATED SUCCESSFULLY: {UserId}", user.Id);

        try
        {
            var customerDto = new CreateCustomerAccountRequest(
                AccountType: 0,
                ImageFile: null,
                PhoneNumber: null,
                Name: user.Name,
                Surname: user.Surname,
                Address: null
            );

            logger.LogInformation("CREATING ACCOUNT for user {UserId}", user.Id);

            var accountDto = await accountService.CreateAccountAsync(customerDto, user.Id);

            logger.LogInformation("ACCOUNT CREATED: {AccountId}", accountDto.Id);

            user.AccountId = Guid.Parse(accountDto.Id);

            var updateResult = await userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                var errors = updateResult.Errors.Select(e => e.Description);

                logger.LogError("USER UPDATE FAILED for {UserId}. Errors: {Errors}",
                    user.Id,
                    string.Join(", ", errors));

                throw new Exception(string.Join(", ", errors));
            }

            logger.LogInformation("USER UPDATED WITH ACCOUNT: {UserId}", user.Id);

            var tokens = await tokenService.GenerateTokensAsync(user);

            logger.LogInformation("TOKENS GENERATED for {UserId}", user.Id);

            return tokens;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "REGISTER PIPELINE FAILED for {UserId}", user.Id);
            throw;
        }
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request)
    {
        logger.LogInformation("LOGIN START for {Email}", request.Email);

        var user = await userManager.FindByEmailAsync(request.Email);

        if (user == null)
        {
            logger.LogWarning("LOGIN FAILED: user not found {Email}", request.Email);
            throw new Exception("Invalid email or password");
        }

        var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password);

        if (!isPasswordValid)
        {
            logger.LogWarning("LOGIN FAILED: wrong password for {Email}", request.Email);
            throw new Exception("Invalid email or password");
        }

        logger.LogInformation("LOGIN SUCCESS for {UserId}", user.Id);

        return await tokenService.GenerateTokensAsync(user);
    }

    public async Task<TokenResponse?> RefreshAsync(string refreshToken)
    {
        logger.LogInformation("REFRESH TOKEN");

        return await tokenService.RefreshAsync(refreshToken);
    }

    public async Task RevokeAsync(string refreshToken)
    {
        logger.LogInformation("REVOKE TOKEN");

        await tokenService.RevokeRefreshTokenAsync(refreshToken);
    }
}
