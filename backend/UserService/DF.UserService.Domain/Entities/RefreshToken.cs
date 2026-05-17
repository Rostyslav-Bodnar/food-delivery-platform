namespace DF.UserService.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string TokenHash { get; set; } = default!;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime Expires { get; set; }
        public DateTime? RevokedAtUtc { get; set; }
        public string? ReplacedByHash { get; set; }

        public bool IsExpired => DateTime.UtcNow >= Expires;
        public bool IsActive => RevokedAtUtc is null && !IsExpired;

        public Guid UserId { get; set; }
        public User User { get; set; } = default!;
    }
}
