namespace Pingr.Application.Abstractions.Services.Authentication;

/// <summary>
/// Provides methods related to password hashing.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes the provided plaintext password.
    /// </summary>
    /// <param name="password">The password to hash.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The hashed password.</returns>
    String HashPassword(string password, CancellationToken cancellationToken);
    
    /// <summary>
    /// Verifies whether the input password and hashed password are identical.
    /// </summary>
    /// <param name="inputPassword">A plaintext password.</param>
    /// <param name="hashedPassword">A hashed password.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    bool VerifyPassword(string inputPassword, string hashedPassword, CancellationToken cancellationToken);
}