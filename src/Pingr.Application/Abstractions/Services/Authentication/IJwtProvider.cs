using Pingr.Domain.Entities;

namespace Pingr.Application.Abstractions.Services.Authentication;

/// <summary>
/// Provides methods related to JWTs.
/// </summary>
public interface IJwtProvider
{
    /// <summary>
    /// Generates a new JWT.
    /// </summary>
    /// <param name="user">The user which data will be used to generate the token.</param>
    /// <returns>The JWT.</returns>
    public string CreateToken(User user);
}