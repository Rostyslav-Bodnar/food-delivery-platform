using DF.UserService.Application.Interfaces;
using DF.UserService.Contracts.Models.DTO;
using DF.UserService.Contracts.Models.Request;
using DF.UserService.Domain.Entities;
using DF.UserService.Infrastructure.Messaging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DF.UserService.Application.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> userManager;
        private readonly IMessageBroker _broker;

        public UserService(UserManager<User> userManager, IMessageBroker broker)
        {
            this.userManager = userManager;
            _broker = broker;
        }

        public async Task<UserDto> RegisterAsync(RegisterRequest request)
        {
            var user = new User { UserName = request.Email, Email = request.Email, FullName = request.FullName };
            var result = await userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

            _broker.Publish("user.registered", new
            {
                user.Id,
                user.Email,
                user.FullName,
                user.CreatedAt
            });

            return new UserDto(user.Id, user.Email, user.FullName);
        }

        public async Task<string> LoginAsync(LoginRequest request)
        {
            // TODO: Validate password, generate JWT
            return "jwt-token";
        }

        public async Task<UserDto> GetUserAsync(Guid userId)
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            return new UserDto(user.Id, user.Email, user.FullName);
        }

        public async Task<List<UserDto>> GetAllUsers()
        {
            var users = await userManager.Users.ToListAsync();
            return users.Select(u => new UserDto(u.Id, u.Email, u.FullName)).ToList();
        }
    }
}
