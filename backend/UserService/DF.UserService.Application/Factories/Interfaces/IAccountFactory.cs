using DF.Contracts.Gateway.Requests.Accounts;
using DF.UserService.Domain.Entities;

namespace DF.UserService.Application.Factories.Interfaces;

public interface IAccountFactory
{
    Task<Account> CreateAccount(CreateAccountRequest request, Guid userId);
}