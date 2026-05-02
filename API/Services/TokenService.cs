using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public class TokenService(IConfiguration configuration)
{
    public string? Generate(string userId, string userName)
    {
        var userClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userName)
        };
        var expiryDate = DateTime.UtcNow.AddMinutes(configuration.GetValue<double>("JwtSettings:ExpiryInMinutes"));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtSettings:Secret"]!));
        var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            expires: expiryDate,
            signingCredentials: cred,
            claims: userClaims,
            issuer: configuration["JwtSettings:Issuer"],
            audience: configuration["JwtSettings:Audience"]
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return jwt;
    }
}