using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DF.Contracts.Gateway.Responses;
using DF.UserService.Application.Services.Interfaces;
using DF.UserService.Domain.Entities;
using DF.UserService.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;


namespace DF.UserService.Application.Services;

public class TokenService(
    IConfiguration config,
    UserManager<User> userManager,
    AppDbContext dbContext,
    ILogger<TokenService> logger)
    : ITokenService
{
    public async Task<TokenResponse> GenerateTokensAsync(User user)
    {
        var jwtSection = config.GetSection("Jwt");
        var refreshDays = jwtSection.GetValue<int>("RefreshTokenExpirationDays");

        var accountType = await ResolveAccountTypeAsync(user.AccountId);
        var (accessToken, validTo) = BuildAccessToken(user, accountType, jwtSection);

        var (refreshToken, refreshHash) = CreateRefreshToken();

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = refreshHash,
            CreatedAtUtc = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(refreshDays),
            UserId = user.Id,
            User = user
        });

        await dbContext.SaveChangesAsync();

        return new TokenResponse(accessToken, refreshToken, validTo);
    }

    public async Task<TokenResponse?> RefreshAsync(string refreshToken)
    {
        var hash = HashToken(refreshToken);

        var stored = await dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash);

        if (stored is null)
        {
            return null;
        }

        if (stored.RevokedAtUtc is not null)
        {
            // Replay of an already-rotated token: revoke the rest of the chain
            // so the legitimate holder is also forced to re-authenticate.
            logger.LogWarning(
                "Refresh-token replay detected for user {UserId}; revoking chain",
                stored.UserId);

            await RevokeChainAsync(stored.ReplacedByHash);
            await dbContext.SaveChangesAsync();
            return null;
        }

        if (stored.IsExpired)
        {
            return null;
        }

        var jwtSection = config.GetSection("Jwt");
        var refreshDays = jwtSection.GetValue<int>("RefreshTokenExpirationDays");

        var (newRefreshToken, newRefreshHash) = CreateRefreshToken();
        var accountType = await ResolveAccountTypeAsync(stored.User.AccountId);
        var (accessToken, validTo) = BuildAccessToken(stored.User, accountType, jwtSection);

        stored.RevokedAtUtc = DateTime.UtcNow;
        stored.ReplacedByHash = newRefreshHash;

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = newRefreshHash,
            CreatedAtUtc = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(refreshDays),
            UserId = stored.UserId,
            User = stored.User
        });

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another refresh call rotated this row first. Treat this call
            // as failed rather than forking the chain; the legitimate caller
            // can retry with the new token if they have it.
            logger.LogWarning(
                "Concurrent refresh detected for user {UserId}; aborting this rotation",
                stored.UserId);
            return null;
        }

        return new TokenResponse(accessToken, newRefreshToken, validTo);
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        var hash = HashToken(refreshToken);

        var stored = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash);

        if (stored is null || stored.RevokedAtUtc is not null)
        {
            return;
        }

        stored.RevokedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
    }

    private (string AccessToken, DateTime ValidTo) BuildAccessToken(
        User user,
        string? accountType,
        IConfigurationSection jwtSection)
    {
        var key = jwtSection.GetValue<string>("Key")!;
        var issuer = jwtSection.GetValue<string>("Issuer")!;
        var audience = jwtSection.GetValue<string>("Audience")!;
        var accessExpMinutes = jwtSection.GetValue<int>("AccessTokenExpirationMinutes");

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        // Issue role under both the framework URI claim and the short "role" name
        // so downstream consumers (Gateway X-Internal-Role forwarder) can read either.
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Role, user.UserRole.ToString()),
            new("role", user.UserRole.ToString()),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? ""),
            new("email", user.Email ?? "")
        };

        // Active account claims — let downstream services authorize against
        // the caller's account (e.g., TrackingService verifies that the
        // business making a location mutation matches account_id).
        if (user.AccountId.HasValue)
        {
            claims.Add(new Claim("account_id", user.AccountId.Value.ToString()));
        }
        if (!string.IsNullOrEmpty(accountType))
        {
            claims.Add(new Claim("account_type", accountType));
        }

        var jwt = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(accessExpMinutes),
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(jwt), jwt.ValidTo);
    }

    private async Task<string?> ResolveAccountTypeAsync(Guid? accountId)
    {
        if (!accountId.HasValue)
        {
            return null;
        }

        return await dbContext.Accounts
            .Where(a => a.Id == accountId.Value)
            .Select(a => a.AccountType.ToString())
            .FirstOrDefaultAsync();
    }

    private async Task RevokeChainAsync(string? startHash)
    {
        var hash = startHash;
        while (!string.IsNullOrEmpty(hash))
        {
            var node = await dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == hash);

            if (node is null)
            {
                break;
            }

            if (node.RevokedAtUtc is null)
            {
                node.RevokedAtUtc = DateTime.UtcNow;
            }

            hash = node.ReplacedByHash;
        }
    }

    private static (string Token, string Hash) CreateRefreshToken()
    {
        var token = GenerateRefreshTokenString();
        return (token, HashToken(token));
    }

    private static string GenerateRefreshTokenString()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
