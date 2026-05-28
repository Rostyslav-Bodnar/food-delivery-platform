using DF.Contracts.Gateway.Requests.Accounts;
using DF.UserService.Application.Factories.Interfaces;
using DF.UserService.Application.Repositories.Interfaces;
using DF.UserService.Application.Services;
using DF.UserService.Application.Services.Interfaces;
using DF.UserService.Contracts.Exceptions;
using DF.UserService.Contracts.Models.DTO;
using DF.UserService.Contracts.Models.Response;
using DF.UserService.Domain.Entities;
using Microsoft.Extensions.Options;
using DomainAccountType = DF.UserService.Domain.Entities.AccountType;
using ContractAccountType = DF.Contracts.Enums.AccountType;

namespace DF.UserService.Tests.Services;

public class AccountServiceTests
{
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IAccountFactory> _factory = new();
    private readonly Mock<ICloudinaryService> _cloudinary = new();
    private readonly Mock<IStripeConnectService> _stripe = new();

    private readonly IOptions<StripeOptions> _stripeOptions = Options.Create(new StripeOptions
    {
        Dashboard = new StripeOptions.DashboardOptions
        {
            ReturnUrl = "https://example.com/return",
            RefreshUrl = "https://example.com/refresh"
        }
    });

    private AccountService CreateSut() => new(
        _accountRepo.Object,
        _userRepo.Object,
        _factory.Object,
        _cloudinary.Object,
        _stripe.Object,
        _stripeOptions);

    private static User MakeUser(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Email = "u@example.com",
        UserName = "u@example.com",
        Name = "U",
        Surname = "Ser",
        UserRole = UserRole.User
    };

    private static CustomerAccount MakeCustomerAccount(Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountType = DomainAccountType.Customer,
        Name = "U",
        Surname = "Ser"
    };

    // ===================
    // CreateAccountAsync
    // ===================

    [Fact]
    public async Task CreateAccountAsync_PromotesNewAccount_OnUserAndReturnsDto()
    {
        var user = MakeUser();
        var newAccount = MakeCustomerAccount(user.Id);
        var request = new CreateCustomerAccountRequest(ContractAccountType.Customer, null, null, "U", "Ser", null);

        _userRepo.Setup(r => r.Get(user.Id)).ReturnsAsync(user);
        _factory.Setup(f => f.CreateAccount(request, user.Id, null)).Returns(newAccount);
        _accountRepo.Setup(r => r.Create(newAccount)).ReturnsAsync(newAccount);
        _userRepo.Setup(r => r.Update(It.IsAny<User>())).Returns(Task.CompletedTask);

        var sut = CreateSut();

        var result = await sut.CreateAccountAsync(request, user.Id);

        result.Should().NotBeNull();
        result.Id.Should().Be(newAccount.Id.ToString());
        result.AccountType.Should().Be(DomainAccountType.Customer.ToString());

        // The user's active account pointer should be flipped to the new account so the
        // next-issued JWT carries account_id / account_type for downstream services.
        user.AccountId.Should().Be(newAccount.Id);
        _userRepo.Verify(r => r.Update(user), Times.Once);
    }

    [Fact]
    public async Task CreateAccountAsync_WrapsUserNotFound_InApplicationException()
    {
        var request = new CreateCustomerAccountRequest(ContractAccountType.Customer, null, null, "U", "Ser", null);
        _userRepo.Setup(r => r.Get(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        var sut = CreateSut();

        var act = async () => await sut.CreateAccountAsync(request, Guid.NewGuid());

        // The service rewraps the inner NotFoundException as ApplicationException.
        var ex = await act.Should().ThrowAsync<ApplicationException>();
        ex.WithInnerException<NotFoundException>().WithMessage("User not found.");
    }

    [Fact]
    public async Task CreateAccountAsync_CleansUpUploadedImage_WhenPersistFails()
    {
        var user = MakeUser();
        var request = new CreateCustomerAccountRequest(
            ContractAccountType.Customer,
            Mock.Of<Microsoft.AspNetCore.Http.IFormFile>(f => f.Length == 100),
            null, "U", "Ser", null);

        var upload = new UploadImageResult("https://cdn/img.png", "public_id_123");
        _userRepo.Setup(r => r.Get(user.Id)).ReturnsAsync(user);
        _cloudinary.Setup(c => c.UploadAsync(It.IsAny<Microsoft.AspNetCore.Http.IFormFile>(), "users"))
            .ReturnsAsync(upload);
        _factory.Setup(f => f.CreateAccount(request, user.Id, upload)).Returns(MakeCustomerAccount(user.Id));
        _accountRepo.Setup(r => r.Create(It.IsAny<Account>())).ThrowsAsync(new InvalidOperationException("db boom"));

        var sut = CreateSut();
        var act = async () => await sut.CreateAccountAsync(request, user.Id);

        await act.Should().ThrowAsync<ApplicationException>();
        _cloudinary.Verify(c => c.DeleteAsync("public_id_123"), Times.Once);
    }

    // ===================
    // DeleteAccountAsync
    // ===================

    [Fact]
    public async Task DeleteAccountAsync_ReturnsTrue_WhenDeleteSucceeds()
    {
        var id = Guid.NewGuid();
        _accountRepo.Setup(r => r.Delete(id)).ReturnsAsync(true);

        var sut = CreateSut();

        var result = await sut.DeleteAccountAsync(id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAccountAsync_WrapsRepoFailure_InApplicationException()
    {
        var id = Guid.NewGuid();
        _accountRepo.Setup(r => r.Delete(id)).ThrowsAsync(new InvalidOperationException("nope"));

        var sut = CreateSut();
        var act = async () => await sut.DeleteAccountAsync(id);

        await act.Should().ThrowAsync<ApplicationException>();
    }

    // ===================
    // GetAccountByUserAsync
    // ===================

    [Fact]
    public async Task GetAccountByUserAsync_ReturnsDto_WhenAccountExists()
    {
        var userId = Guid.NewGuid();
        var account = MakeCustomerAccount(userId);
        _accountRepo.Setup(r => r.GetAccountByUserAsync(userId)).ReturnsAsync(account);

        var sut = CreateSut();

        var result = await sut.GetAccountByUserAsync(userId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(account.Id.ToString());
    }

    [Fact]
    public async Task GetAccountByUserAsync_ReturnsNull_WhenAccountMissing()
    {
        _accountRepo.Setup(r => r.GetAccountByUserAsync(It.IsAny<Guid>())).ReturnsAsync((Account?)null);

        var sut = CreateSut();

        var result = await sut.GetAccountByUserAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ===================
    // GetAccountsByUserAsync
    // ===================

    [Fact]
    public async Task GetAccountsByUserAsync_ReturnsAllUserAccounts()
    {
        var userId = Guid.NewGuid();
        var a1 = MakeCustomerAccount(userId);
        var a2 = new BusinessAccount
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AccountType = DomainAccountType.Business,
            Name = "Biz"
        };
        _accountRepo.Setup(r => r.GetAccountsByUserAsync(userId))
            .ReturnsAsync(new Account?[] { a1, a2 });

        var sut = CreateSut();

        var result = (await sut.GetAccountsByUserAsync(userId))!.ToList();

        result.Should().HaveCount(2);
        result.Select(x => x.AccountType).Should().Contain(new[] { "Customer", "Business" });
    }

    [Fact]
    public async Task GetAccountsByUserAsync_ReturnsEmpty_WhenNone()
    {
        _accountRepo.Setup(r => r.GetAccountsByUserAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Array.Empty<Account?>());

        var sut = CreateSut();

        var result = await sut.GetAccountsByUserAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    // ===================
    // GetBusinessAccountsAsync
    // ===================

    [Fact]
    public async Task GetBusinessAccountsAsync_ReturnsOnlyBusinessAccounts()
    {
        var userId = Guid.NewGuid();
        var customer = MakeCustomerAccount(userId);
        var business = new BusinessAccount
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AccountType = DomainAccountType.Business,
            Name = "Biz"
        };
        _accountRepo.Setup(r => r.GetAll())
            .ReturnsAsync(new Account?[] { customer, business });

        var sut = CreateSut();

        var result = (await sut.GetBusinessAccountsAsync())!.ToList();

        result.Should().ContainSingle()
            .Which.AccountType.Should().Be(DomainAccountType.Business.ToString());
    }

    // ===================
    // GetOnboardingLinkAsync
    // ===================

    [Fact]
    public async Task GetOnboardingLinkAsync_ReturnsProvisioning_WhenStripeIdMissing()
    {
        var biz = new BusinessAccount
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            AccountType = DomainAccountType.Business,
            Name = "Biz",
            StripeAccountId = null
        };
        _accountRepo.Setup(r => r.Get(biz.Id)).ReturnsAsync(biz);

        var sut = CreateSut();

        var result = await sut.GetOnboardingLinkAsync(biz.Id, CancellationToken.None);

        result.Status.Should().Be(OnboardingLinkResponse.ProvisioningStatus);
        result.Url.Should().BeNull();
    }

    [Fact]
    public async Task GetOnboardingLinkAsync_ReturnsReadyWithUrl_WhenStripeAccountReady()
    {
        var biz = new BusinessAccount
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            AccountType = DomainAccountType.Business,
            Name = "Biz",
            StripeAccountId = "acct_123"
        };
        _accountRepo.Setup(r => r.Get(biz.Id)).ReturnsAsync(biz);
        _stripe.Setup(s => s.CreateOnboardingLinkAsync(
                "acct_123",
                "https://example.com/return",
                "https://example.com/refresh",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://stripe.example/onboarding");

        var sut = CreateSut();

        var result = await sut.GetOnboardingLinkAsync(biz.Id, CancellationToken.None);

        result.Status.Should().Be(OnboardingLinkResponse.ReadyStatus);
        result.Url.Should().Be("https://stripe.example/onboarding");
    }

    [Fact]
    public async Task GetOnboardingLinkAsync_Throws_WhenAccountNotFound()
    {
        _accountRepo.Setup(r => r.Get(It.IsAny<Guid>())).ReturnsAsync((Account?)null);

        var sut = CreateSut();
        var act = async () => await sut.GetOnboardingLinkAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Business not found");
    }
}
