using Microsoft.AspNetCore.Identity;

namespace DF.UserService.Domain.Entities
{
    public class User : IdentityUser<Guid>
    {
        public required string FullName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
