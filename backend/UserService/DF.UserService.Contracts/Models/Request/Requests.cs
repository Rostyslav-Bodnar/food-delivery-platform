namespace DF.UserService.Contracts.Models.Request
{
    public record RegisterRequest(string Email, string Password, string Name, string Surname);
    public record LoginRequest(string Email, string Password);

}
