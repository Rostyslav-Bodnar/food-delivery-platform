using DF.UserService.Contracts.Models.DTO;
using DF.UserService.Domain.Entities;

namespace DF.UserService.Application.Factories.Interfaces;

public interface IAccountFactory
{
    Account CreateAccount(AccountDTO dto);
}