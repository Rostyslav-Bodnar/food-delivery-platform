using System.Security.Claims;
using System.Text;
using DF.OrderService.Application.Services.Interfaces;
using DF.OrderService.Contracts.Models.Responses;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace DF.OrderService.Application.Services;

public class TrackingTokenService(IConfiguration configuration) : ITrackingTokenService
{
    public TrackingAccessTokenResponse CreateTrackingToken(
        Guid subjectId,
        string role,
        Guid orderId,
        IReadOnlyCollection<string> scopes,
        TimeSpan lifetime)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)
        );

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        var expires = now.Add(lifetime);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subjectId.ToString()),
            new("role", role),
            new("order_id", orderId.ToString()),
            new("scope", string.Join(' ', scopes)),
            new(JwtRegisteredClaimNames.Iss, "df.orderservice"),
            new(JwtRegisteredClaimNames.Aud, "df.tracking")
        };

        var token = new JwtSecurityToken(
            issuer: "df.orderservice",
            audience: "df.tracking",
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return new TrackingAccessTokenResponse(jwt, expires);
    }
}
