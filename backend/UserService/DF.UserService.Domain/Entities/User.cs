using Microsoft.AspNetCore.Identity;

namespace DF.UserService.Domain.Entities
{
    public class User : IdentityUser<Guid>
    {
        public required string Name { get; set; }
        public required string Surname { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        //TODO: location implementation
        
        public Guid? AccountId { get; set; }
        public Account? CurrentAccount { get; set; }
        
        public required UserRole UserRole { get; set; }
        
        public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

    }
}
