using System.ComponentModel.DataAnnotations;

namespace DF.Contracts.Gateway.Requests.Auth;

public record RegisterRequest(
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, StringLength(128, MinimumLength = 8)] string Password,
    [Required, StringLength(200)] string Name,
    [Required, StringLength(200)] string Surname);

public record LoginRequest(
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, StringLength(128)] string Password);
