using DF.UserService.Application.Services;
using DF.UserService.Domain.Entities;
using DF.UserService.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DF.UserService.Tests.Services;

public class UserServiceTests
{
    private static User MakeUser(string email = "alice@example.com", UserRole role = UserRole.User)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            Name = "Alice",
            Surname = "Smith",
            UserRole = role
        };
    }

    [Fact]
    public async Task GetUserAsync_ReturnsUserDto_WhenUserExists()
    {
        await using var db = DbContextFactory.CreateInMemory();
        var user = MakeUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var userManager = MockUserManager.Create();
        var sut = new Application.Services.UserService(userManager.Object, db);

        var result = await sut.GetUserAsync(user.Id);

        result.Should().NotBeNull();
        result.Id.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
        result.Name.Should().Be(user.Name);
        result.Surname.Should().Be(user.Surname);
        result.UserRole.Should().Be(UserRole.User.ToString());
        result.CurrentAccount.Should().BeNull();
    }

    [Fact]
    public async Task GetUserAsync_IncludesCurrentAccount_WhenLinked()
    {
        await using var db = DbContextFactory.CreateInMemory();
        var user = MakeUser();
        var account = new CustomerAccount
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            AccountType = AccountType.Customer,
            Name = "Alice",
            Surname = "Smith"
        };
        user.AccountId = account.Id;
        db.Users.Add(user);
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        var sut = new Application.Services.UserService(MockUserManager.Create().Object, db);

        var result = await sut.GetUserAsync(user.Id);

        result.CurrentAccount.Should().NotBeNull();
        result.CurrentAccount.AccountType.Should().Be(AccountType.Customer.ToString());
    }

    [Fact]
    public async Task GetUserAsync_Throws_WhenUserMissing()
    {
        await using var db = DbContextFactory.CreateInMemory();
        var sut = new Application.Services.UserService(MockUserManager.Create().Object, db);

        var act = async () => await sut.GetUserAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("User not found");
    }

    [Fact]
    public async Task GetAllUsers_ReturnsAllPersistedUsers()
    {
        await using var db = DbContextFactory.CreateInMemory();
        db.Users.AddRange(MakeUser("a@x.com"), MakeUser("b@x.com"), MakeUser("c@x.com"));
        await db.SaveChangesAsync();

        var sut = new Application.Services.UserService(MockUserManager.Create().Object, db);

        var result = await sut.GetAllUsers();

        result.Should().HaveCount(3);
        result.Select(u => u.Email).Should().BeEquivalentTo(new[] { "a@x.com", "b@x.com", "c@x.com" });
    }

    [Fact]
    public async Task GetAllUsers_ReturnsEmptyList_WhenNoneExist()
    {
        await using var db = DbContextFactory.CreateInMemory();
        var sut = new Application.Services.UserService(MockUserManager.Create().Object, db);

        var result = await sut.GetAllUsers();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserEntityAsync_ReturnsUser_WithAccounts()
    {
        await using var db = DbContextFactory.CreateInMemory();
        var user = MakeUser();
        var account = new CustomerAccount
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            AccountType = AccountType.Customer,
            Name = "Alice",
            Surname = "Smith"
        };
        user.Accounts.Add(account);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var userManager = MockUserManager.Create();
        userManager.Setup(m => m.Users).Returns(db.Users);

        var sut = new Application.Services.UserService(userManager.Object, db);

        var result = await sut.GetUserEntityAsync(user.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Accounts.Should().ContainSingle().Which.Id.Should().Be(account.Id);
    }

    [Fact]
    public async Task GetUserEntityAsync_ReturnsNull_WhenMissing()
    {
        await using var db = DbContextFactory.CreateInMemory();
        var userManager = MockUserManager.Create();
        userManager.Setup(m => m.Users).Returns(db.Users);

        var sut = new Application.Services.UserService(userManager.Object, db);

        var result = await sut.GetUserEntityAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateUserAsync_CallsUserManagerUpdate()
    {
        var user = MakeUser();
        var userManager = MockUserManager.Create();
        userManager
            .Setup(m => m.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success)
            .Verifiable();

        await using var db = DbContextFactory.CreateInMemory();
        var sut = new Application.Services.UserService(userManager.Object, db);

        await sut.UpdateUserAsync(user);

        userManager.Verify();
    }
}
