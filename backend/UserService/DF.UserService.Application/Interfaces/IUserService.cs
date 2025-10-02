using DF.UserService.Contracts.Models.DTO;
using DF.UserService.Contracts.Models.Request;

namespace DF.UserService.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserDto> GetUserAsync(Guid userId);
        Task<List<UserDto>> GetAllUsers();
    }
}
