using DF.Contracts.Gateway.Requests.Accounts;
using DF.UserService.Contracts.Models.Response;
using DF.UserService.Domain.Entities;

namespace DF.UserService.Application.Factories.Interfaces;

public interface IAccountFactory
{
    Account CreateAccount(CreateAccountRequest request, Guid userId, UploadImageResult? image);
}
