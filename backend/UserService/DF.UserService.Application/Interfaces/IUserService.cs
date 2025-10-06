using DF.UserService.Contracts.Models.DTO;
using DF.UserService.Contracts.Models.Request;
using DF.UserService.Domain.Entities;

namespace DF.UserService.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserDto> GetUserAsync(Guid userId);
        Task<List<UserDto>> GetAllUsers();
        Task UpdateUserAsync(User user);
        Task<User?> GetUserEntityAsync(Guid userId);

    }
}
