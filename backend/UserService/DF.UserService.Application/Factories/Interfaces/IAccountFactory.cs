using DF.UserService.Contracts.Models.DTO;
using DF.UserService.Contracts.Models.Request;
using DF.UserService.Domain.Entities;

namespace DF.UserService.Application.Factories.Interfaces;

public interface IAccountFactory
{
    Task<Account> CreateAccount(CreateAccountRequest request, Guid userId);
}