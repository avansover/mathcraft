using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Mathcraft.Server.Models;
using Microsoft.IdentityModel.Tokens;

namespace Mathcraft.Server.Common;

public interface IJwtTokenService
{
    string GenerateAccessToken(FamilyAccount account);
}

public class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public string GenerateAccessToken(FamilyAccount account)
    {
        var secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT secret is not configured.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiryMinutes = configuration.GetValue<int>("Jwt:AccessTokenExpiryMinutes", 15);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, account.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
