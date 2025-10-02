using DF.UserService.Application.Interfaces;
using DF.UserService.Application.Mappers;
using DF.UserService.Contracts.Models.DTO;
using DF.UserService.Domain.Entities;
using DF.UserService.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DF.UserService.Application.Services
{
    public class UserService(UserManager<User> userManager, AppDbContext dbContext) : IUserService
    {
        public async Task<UserDto> GetUserAsync(Guid userId)
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user == null) throw new Exception("User not found");

            await dbContext.Entry(user).Reference(u => u.CurrentAccount).LoadAsync();

            return new UserDto(user.Id, user.Email, user.FullName, user.UserRole.ToString(), AccountMapper.ToDTO(user.CurrentAccount));
        }

        public async Task<List<UserDto>> GetAllUsers()
        {
            var users = await userManager.Users.ToListAsync();
            return users.Select(u => new UserDto(u.Id, u.Email, u.FullName, u.UserRole.ToString(), AccountMapper.ToDTO(u.CurrentAccount))).ToList();
        }
    }
}
