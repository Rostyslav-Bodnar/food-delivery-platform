namespace DF.UserService.Contracts.Models.Request
{
    public record RegisterRequest(string Email, string Password, string FullName);
    public record LoginRequest(string Email, string Password);
    
    public class RefreshRequest
    {
        public string RefreshToken { get; set; } = default!;
    }

}
