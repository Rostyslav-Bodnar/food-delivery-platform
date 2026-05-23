using DF.Contracts.Gateway.Responses;
using DF.UserService.Application.Mappers;
using DF.UserService.Application.Services.Interfaces;
using DF.UserService.Domain.Entities;
using DF.UserService.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DF.UserService.Application.Services
{
    public class UserService(UserManager<User> userManager, AppDbContext dbContext) : IUserService
    {
        private const int DefaultPageSize = 100;

        public async Task<UserDto> GetUserAsync(Guid userId)
        {
            var user = await dbContext.Users
                .Include(u => u.CurrentAccount)
                .FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new KeyNotFoundException("User not found");

            return new UserDto(
                user.Id,
                user.Email,
                user.Name,
                user.Surname,
                user.UserRole.ToString(),
                user.CurrentAccount is null ? null! : AccountMapper.ToDTO(user.CurrentAccount));
        }

        public async Task<List<UserDto>> GetAllUsers()
        {
            var users = await dbContext.Users
                .Include(u => u.CurrentAccount)
                .OrderBy(u => u.Id)
                .Take(DefaultPageSize)
                .ToListAsync();

            return users.Select(u => new UserDto(
                u.Id,
                u.Email,
                u.Name,
                u.Surname,
                u.UserRole.ToString(),
                u.CurrentAccount is null ? null! : AccountMapper.ToDTO(u.CurrentAccount))).ToList();
        }

        public async Task<User?> GetUserEntityAsync(Guid userId)
        {
            return await userManager.Users.Include(u => u.Accounts).FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task UpdateUserAsync(User user)
        {
            await userManager.UpdateAsync(user);
        }
    }
}
