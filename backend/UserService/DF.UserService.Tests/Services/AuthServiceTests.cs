using DF.Contracts.Gateway.Requests.Accounts;
using DF.Contracts.Gateway.Requests.Auth;
using DF.Contracts.Gateway.Responses;
using DF.UserService.Application.Services;
using DF.UserService.Application.Services.Interfaces;
using DF.UserService.Domain.Entities;
using DF.UserService.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;

namespace DF.UserService.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IAccountService> _accountService = new();

    private static TokenResponse SomeTokens() =>
        new("access-token", "refresh-token", DateTime.UtcNow.AddMinutes(30));

    // ===================
    // RegisterAsync
    // ===================

    [Fact]
    public async Task RegisterAsync_CreatesUser_LinksCustomerAccount_ReturnsTokens()
    {
        await using var db = DbContextFactory.CreateInMemory();
        var userManager = MockUserManager.Create();

        userManager.Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(m => m.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        var accountId = Guid.NewGuid();
        _accountService
            .Setup(a => a.CreateAccountAsync(It.IsAny<CreateAccountRequest>(), It.IsAny<Guid>()))
            .ReturnsAsync(new CustomerAccountResponse
            {
                Id = accountId.ToString(),
                UserId = Guid.NewGuid().ToString(),
                AccountType = "Customer"
            });

        _tokenService.Setup(t => t.GenerateTokensAsync(It.IsAny<User>()))
            .ReturnsAsync(SomeTokens());

        var sut = new AuthService(
            userManager.Object,
            _tokenService.Object,
            _accountService.Object,
            db,
            NullLogger<AuthService>.Instance);

        var result = await sut.RegisterAsync(new RegisterRequest("a@x.com", "Password1!", "Alice", "Smith"));

        result.AccessToken.Should().Be("access-token");
        // The user passed to GenerateTokensAsync should already have AccountId pointing at the new account.
        _tokenService.Verify(t => t.GenerateTokensAsync(
            It.Is<User>(u => u.AccountId == accountId)), Times.Once);
        userManager.Verify(m => m.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_Throws_WhenUserCreationFails()
    {
        await using var db = DbContextFactory.CreateInMemory();
        var userManager = MockUserManager.Create();

        userManager.Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordTooShort",
                Description = "Password too short"
            }));

        var sut = new AuthService(
            userManager.Object,
            _tokenService.Object,
            _accountService.Object,
            db,
            NullLogger<AuthService>.Instance);

        var act = async () =>
            await sut.RegisterAsync(new RegisterRequest("a@x.com", "short", "Alice", "Smith"));

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Password too short*");

        // Account flow should not have started.
        _accountService.Verify(
            a => a.CreateAccountAsync(It.IsAny<CreateAccountRequest>(), It.IsAny<Guid>()),
            Times.Never);
        _tokenService.Verify(t => t.GenerateTokensAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_Throws_WhenUserUpdateAfterAccountCreationFails()
    {
        await using var db = DbContextFactory.CreateInMemory();
        var userManager = MockUserManager.Create();

        userManager.Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(m => m.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Code = "ConcurrencyFailure",
                Description = "Update failed"
            }));

        _accountService
            .Setup(a => a.CreateAccountAsync(It.IsAny<CreateAccountRequest>(), It.IsAny<Guid>()))
            .ReturnsAsync(new CustomerAccountResponse
            {
                Id = Guid.NewGuid().ToString(),
                UserId = Guid.NewGuid().ToString(),
                AccountType = "Customer"
            });

        var sut = new AuthService(
            userManager.Object,
            _tokenService.Object,
            _accountService.Object,
            db,
            NullLogger<AuthService>.Instance);

        var act = async () =>
            await sut.RegisterAsync(new RegisterRequest("a@x.com", "Password1!", "Alice", "Smith"));

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Update failed*");

        _tokenService.Verify(t => t.GenerateTokensAsync(It.IsAny<User>()), Times.Never);
    }

    // ===================
    // LoginAsync
    // ===================

    private static User MakeUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "a@x.com",
        UserName = "a@x.com",
        Name = "Alice",
        Surname = "Smith",
        UserRole = UserRole.User
    };

    [Fact]
    public async Task LoginAsync_ReturnsTokens_AndResetsFailedAttempts_OnSuccess()
    {
        await using var db = DbContextFactory.CreateInMemory();
        var user = MakeUser();
        var userManager = MockUserManager.Create();

        userManager.Setup(m => m.FindByEmailAsync("a@x.com")).ReturnsAsync(user);
        userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        userManager.Setup(m => m.CheckPasswordAsync(user, "secret")).ReturnsAsync(true);
        userManager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);

        _tokenService.Setup(t => t.GenerateTokensAsync(user)).ReturnsAsync(SomeTokens());

        var sut = new AuthService(
            userManager.Object,
            _tokenService.Object,
            _accountService.Object,
            db,
            NullLogger<AuthService>.Instance);

        var result = await sut.LoginAsync(new LoginRequest("a@x.com", "secret"));

        result.AccessToken.Should().Be("access-token");
        userManager.Verify(m => m.ResetAccessFailedCountAsync(user), Times.Once);
        userManager.Verify(m => m.AccessFailedAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_Throws_WhenUserNotFound()
    {
        await using var db = DbContextFactory.CreateInMemory();
        var userManager = MockUserManager.Create();
        userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var sut = new AuthService(
            userManager.Object,
            _tokenService.Object,
            _accountService.Object,
            db,
            NullLogger<AuthService>.Instance);

        var act = async () => await sut.LoginAsync(new LoginRequest("ghost@x.com", "pw"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_Throws_WhenAccountLocked()
    {
        await using var db = DbContextFactory.CreateInMemory();
        var user = MakeUser();
        var userManager = MockUserManager.Create();

        userManager.Setup(m => m.FindByEmailAsync("a@x.com")).ReturnsAsync(user);
        userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true);

        var sut = new AuthService(
            userManager.Object,
            _tokenService.Object,
            _accountService.Object,
            db,
            NullLogger<AuthService>.Instance);

        var act = async () => await sut.LoginAsync(new LoginRequest("a@x.com", "pw"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Account temporarily locked. Try again later.");

        // Locked-account branch shouldn't even check the password.
        userManager.Verify(m => m.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_IncrementsFailedAttempts_AndThrows_OnWrongPassword()
    {
        await using var db = DbContextFactory.CreateInMemory();
        var user = MakeUser();
        var userManager = MockUserManager.Create();

        userManager.Setup(m => m.FindByEmailAsync("a@x.com")).ReturnsAsync(user);
        userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        userManager.Setup(m => m.CheckPasswordAsync(user, "wrong")).ReturnsAsync(false);
        userManager.Setup(m => m.AccessFailedAsync(user)).ReturnsAsync(IdentityResult.Success);

        var sut = new AuthService(
            userManager.Object,
            _tokenService.Object,
            _accountService.Object,
            db,
            NullLogger<AuthService>.Instance);

        var act = async () => await sut.LoginAsync(new LoginRequest("a@x.com", "wrong"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid email or password");

        userManager.Verify(m => m.AccessFailedAsync(user), Times.Once);
        _tokenService.Verify(t => t.GenerateTokensAsync(It.IsAny<User>()), Times.Never);
    }

    // ===================
    // RefreshAsync / RevokeAsync (pass-throughs)
    // ===================

    [Fact]
    public async Task RefreshAsync_DelegatesToTokenService()
    {
        await using var db = DbContextFactory.CreateInMemory();
        var expected = SomeTokens();
        _tokenService.Setup(t => t.RefreshAsync("refresh-1")).ReturnsAsync(expected);

        var sut = new AuthService(
            MockUserManager.Create().Object,
            _tokenService.Object,
            _accountService.Object,
            db,
            NullLogger<AuthService>.Instance);

        var result = await sut.RefreshAsync("refresh-1");

        result.Should().BeSameAs(expected);
        _tokenService.Verify(t => t.RefreshAsync("refresh-1"), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsNull_WhenTokenServiceReturnsNull()
    {
        await using var db = DbContextFactory.CreateInMemory();
        _tokenService.Setup(t => t.RefreshAsync(It.IsAny<string>())).ReturnsAsync((TokenResponse?)null);

        var sut = new AuthService(
            MockUserManager.Create().Object,
            _tokenService.Object,
            _accountService.Object,
            db,
            NullLogger<AuthService>.Instance);

        var result = await sut.RefreshAsync("anything");

        result.Should().BeNull();
    }

    [Fact]
    public async Task RevokeAsync_DelegatesToTokenService()
    {
        await using var db = DbContextFactory.CreateInMemory();
        _tokenService.Setup(t => t.RevokeRefreshTokenAsync("refresh-1")).Returns(Task.CompletedTask);

        var sut = new AuthService(
            MockUserManager.Create().Object,
            _tokenService.Object,
            _accountService.Object,
            db,
            NullLogger<AuthService>.Instance);

        await sut.RevokeAsync("refresh-1");

        _tokenService.Verify(t => t.RevokeRefreshTokenAsync("refresh-1"), Times.Once);
    }
}
