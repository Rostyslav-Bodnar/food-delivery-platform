using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using DF.UserService.Application.Services;
using DF.UserService.Domain.Entities;
using DF.UserService.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace DF.UserService.Tests.Services;

public class TokenServiceTests
{
    private static IConfiguration BuildConfig() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "this-is-a-very-long-test-only-signing-key-256bits!",
            ["Jwt:Issuer"] = "df-tests",
            ["Jwt:Audience"] = "df-tests-audience",
            ["Jwt:AccessTokenExpirationMinutes"] = "30",
            ["Jwt:RefreshTokenExpirationDays"] = "7"
        })
        .Build();

    private static User MakeUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "u@example.com",
        UserName = "u@example.com",
        Name = "U",
        Surname = "Ser",
        UserRole = UserRole.User
    };

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    [Fact]
    public async Task GenerateTokensAsync_ProducesAccessAndRefreshToken_AndPersistsHash()
    {
        await using var db = DbContextFactory.CreateInMemory();
        var user = MakeUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sut = new TokenService(
            BuildConfig(),
            MockUserManager.Create().Object,
            db,
            NullLogger<TokenService>.Instance);

        var tokens = await sut.GenerateTokensAsync(user);

        tokens.Should().NotBeNull();
        tokens.AccessToken.Should().NotBeNullOrEmpty();
        tokens.RefreshToken.Should().NotBeNullOrEmpty();
        tokens.AccessTokenExpiresAt.Should().BeAfter(DateTime.UtcNow);

        db.RefreshTokens.Should().ContainSingle()
            .Which.TokenHash.Should().Be(HashToken(tokens.RefreshToken));
    }

    [Fact]
    public async Task GenerateTokensAsync_EmbedsUserClaims_InAccessToken()
    {
        await using var db = DbContextFactory.CreateInMemory();
        var user = MakeUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sut = new TokenService(
            BuildConfig(),
            MockUserManager.Create().Object,
            db,
            NullLogger<TokenService>.Instance);

        var tokens = await sut.GenerateTokensAsync(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken);
        jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "User");
        jwt.Claims.Should().Contain(c => c.Type == "email" && c.Value == user.Email);
        jwt.Claims.Should().Contain(c => c.Type == "sub" && c.Value == user.Id.ToString());
    }

    [Fact]
    public async Task GenerateTokensAsync_EmbedsAccountClaims_WhenAccountAssigned()
    {
        await using var db = DbContextFactory.CreateInMemory();
        var user = MakeUser();
        var account = new BusinessAccount
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            AccountType = AccountType.Business,
            Name = "Acme"
        };
        user.AccountId = account.Id;
        db.Users.Add(user);
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        var sut = new TokenService(
            BuildConfig(),
            MockUserManager.Create().Object,
            db,
            NullLogger<TokenService>.Instance);

        var tokens = await sut.GenerateTokensAsync(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken);
        jwt.Claims.Should().Contain(c => c.Type == "account_id" && c.Value == account.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "account_type" && c.Value == "Business");
    }

    [Fact]
    public async Task RefreshAsync_ReturnsNull_WhenTokenUnknown()
    {
        await using var db = DbContextFactory.CreateInMemory();
        var sut = new TokenService(
            BuildConfig(),
            MockUserManager.Create().Object,
            db,
            NullLogger<TokenService>.Instance);

        var result = await sut.RefreshAsync("not-a-real-token");

        result.Should().BeNull();
    }

    [Fact]
    public async Task RefreshAsync_RotatesToken_AndRevokesOldRow_OnHappyPath()
    {
        await using var db = DbContextFactory.CreateInMemory();
        var user = MakeUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sut = new TokenService(
            BuildConfig(),
            MockUserManager.Create().Object,
            db,
            NullLogger<TokenService>.Instance);

        var original = await sut.GenerateTokensAsync(user);
        var rotated = await sut.RefreshAsync(original.RefreshToken);

        rotated.Should().NotBeNull();
        rotated!.RefreshToken.Should().NotBe(original.RefreshToken);

        var oldRow = db.RefreshTokens.Single(r => r.TokenHash == HashToken(original.RefreshToken));
        oldRow.RevokedAtUtc.Should().NotBeNull();
        oldRow.ReplacedByHash.Should().Be(HashToken(rotated.RefreshToken));
    }

    [Fact]
    public async Task RefreshAsync_DetectsReplay_AndRevokesChain()
    {
        await using var db = DbContextFactory.CreateInMemory();
        var user = MakeUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sut = new TokenService(
            BuildConfig(),
            MockUserManager.Create().Object,
            db,
            NullLogger<TokenService>.Instance);

        var original = await sut.GenerateTokensAsync(user);
        var rotated = await sut.RefreshAsync(original.RefreshToken);

        // Replay: try the already-rotated token again.
        var replay = await sut.RefreshAsync(original.RefreshToken);

        replay.Should().BeNull();
        var newRow = db.RefreshTokens.Single(r => r.TokenHash == HashToken(rotated!.RefreshToken));
        newRow.RevokedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_MarksRowRevoked()
    {
        await using var db = DbContextFactory.CreateInMemory();
        var user = MakeUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sut = new TokenService(
            BuildConfig(),
            MockUserManager.Create().Object,
            db,
            NullLogger<TokenService>.Instance);

        var tokens = await sut.GenerateTokensAsync(user);
        await sut.RevokeRefreshTokenAsync(tokens.RefreshToken);

        var row = db.RefreshTokens.Single(r => r.TokenHash == HashToken(tokens.RefreshToken));
        row.RevokedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_IsNoOp_WhenTokenUnknown()
    {
        await using var db = DbContextFactory.CreateInMemory();
        var sut = new TokenService(
            BuildConfig(),
            MockUserManager.Create().Object,
            db,
            NullLogger<TokenService>.Instance);

        var act = async () => await sut.RevokeRefreshTokenAsync("unknown");

        await act.Should().NotThrowAsync();
        db.RefreshTokens.Should().BeEmpty();
    }
}
