using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DF.UserService.Application.Services.Interfaces;
using DF.UserService.Contracts.Models.Response;
using DF.UserService.Domain.Entities;
using DF.UserService.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;


namespace DF.UserService.Application.Services;

public class TokenService(IConfiguration config, UserManager<User> userManager, AppDbContext dbContext)
    : ITokenService
{
    public async Task<TokenResponse> GenerateTokensAsync(User user)
    {
        var jwtSection = config.GetSection("Jwt");
        var key = jwtSection.GetValue<string>("Key")!;
        var issuer = jwtSection.GetValue<string>("Issuer")!;
        var audience = jwtSection.GetValue<string>("Audience")!;
        var accessExpMinutes = jwtSection.GetValue<int>("AccessTokenExpirationMinutes");
        var refreshDays = jwtSection.GetValue<int>("RefreshTokenExpirationDays");

        var keyBytes = Encoding.UTF8.GetBytes(key);
        var signingKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.UserRole.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? ""),
            new Claim("email", user.Email ?? "")
            // add more claims if needed (roles etc.)
        };

        var jwt = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(accessExpMinutes),
            signingCredentials: credentials
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

        // generate secure refresh token (random)
        var refreshToken = GenerateRefreshTokenString();
        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            Expires = DateTime.UtcNow.AddDays(refreshDays),
            UserId = user.Id,
            User = user
        };

        dbContext.RefreshTokens.Add(refreshTokenEntity);
        await dbContext.SaveChangesAsync();

        return new TokenResponse(accessToken, refreshToken, jwt.ValidTo);
    }

    public async Task<TokenResponse?> RefreshAsync(string refreshToken)
    {
        var stored = await dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (stored == null || stored.IsExpired)
            return null;

        // optionally: invalidate old refresh token (rotate)
        dbContext.RefreshTokens.Remove(stored);
        await dbContext.SaveChangesAsync();

        // create new tokens
        return await GenerateTokensAsync(stored.User);
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        var stored = await dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);
        if (stored != null)
        {
            dbContext.RefreshTokens.Remove(stored);
            await dbContext.SaveChangesAsync();
        }
    }

    private static string GenerateRefreshTokenString()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}