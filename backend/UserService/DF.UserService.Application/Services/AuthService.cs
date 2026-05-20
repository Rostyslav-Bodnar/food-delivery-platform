using DF.Contracts.Gateway.Requests.Auth;
using DF.Contracts.Gateway.Responses;
using DF.UserService.Application.Services.Interfaces;
using DF.UserService.Domain.Entities;
using DF.UserService.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using CreateCustomerAccountRequest = DF.Contracts.Gateway.Requests.Accounts.CreateCustomerAccountRequest;

namespace DF.UserService.Application.Services;


public class AuthService(
    UserManager<User> userManager,
    ITokenService tokenService,
    IAccountService accountService,
    AppDbContext dbContext,
    ILogger<AuthService> logger)
    : IAuthService
{
    public async Task<TokenResponse> RegisterAsync(RegisterRequest request)
    {
        logger.LogInformation("REGISTER START for {Email}", request.Email);

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
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
            {
                var errors = result.Errors.Select(e => e.Description).ToList();

                logger.LogWarning("USER CREATION FAILED for {Email}. Errors: {Errors}",
                    request.Email,
                    string.Join(", ", errors));

                throw new ArgumentException(string.Join(", ", errors));
            }

            logger.LogInformation("USER CREATED: {UserId}", user.Id);

            var customerDto = new CreateCustomerAccountRequest(
                AccountType: 0,
                ImageFile: null,
                PhoneNumber: null,
                Name: user.Name,
                Surname: user.Surname,
                Address: null
            );

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

                throw new ArgumentException(string.Join(", ", errors));
            }

            var tokens = await tokenService.GenerateTokensAsync(user);

            await transaction.CommitAsync();

            logger.LogInformation("REGISTER SUCCESS for {UserId}", user.Id);

            return tokens;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "REGISTER PIPELINE FAILED for {Email}; rolling back", request.Email);
            await transaction.RollbackAsync();
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
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            logger.LogWarning("LOGIN BLOCKED: account locked {UserId}", user.Id);
            throw new UnauthorizedAccessException("Account temporarily locked. Try again later.");
        }

        var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password);

        if (!isPasswordValid)
        {
            // Increment failed-access count; locks the account once MaxFailedAccessAttempts is hit.
            await userManager.AccessFailedAsync(user);
            logger.LogWarning("LOGIN FAILED: wrong password for {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // Successful login resets the failed-attempt counter.
        await userManager.ResetAccessFailedCountAsync(user);
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
