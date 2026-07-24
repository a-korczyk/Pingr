using System.Security.Claims;
using System.Text;
using Pingr.Application.Abstractions.Services;
using Pingr.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Pingr.Application.Abstractions.Services.Authentication;
using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

namespace Pingr.Infrastructure.Services.Authentication;

/// <summary>
/// Implementation of <see cref="IJwtProvider"/>
/// </summary>
public class JwtProvider(
    IOptions<AccessTokenOptions> accessTokenOptions) : IJwtProvider
{
    private readonly AccessTokenOptions _accessTokenOptions = accessTokenOptions.Value;
    
    public string CreateToken(User user)
    {
        string secretKey = _accessTokenOptions.Secret;
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
            ]),
            Expires = DateTime.UtcNow.AddMinutes(_accessTokenOptions.ExpirationInMinutes),
            SigningCredentials = credentials,
            Issuer = _accessTokenOptions.Issuer,
            Audience = _accessTokenOptions.Audience
        };

        var handler = new JsonWebTokenHandler();

        string token = handler.CreateToken(tokenDescriptor);
        return token;
    }
}