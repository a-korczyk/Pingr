using System.Security.Cryptography;
using System.Text;
using Pingr.Application.Abstractions.Services;
using Microsoft.AspNetCore.WebUtilities;
using Pingr.Application.Abstractions.Services.Authentication;

namespace Pingr.Infrastructure.Services.Authentication;

/// <inheritdoc/>
public sealed class TokenGenerator() : ITokenGenerator
{
    public string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    /// <remarks>
    /// Uses SHA256 as the hashing algorithm and returns it in Hex string.
    /// </remarks>
    public string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}