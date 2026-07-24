using Pingr.Domain.Entities;

namespace Pingr.Application.Abstractions.Services.Authentication;

/// <summary>
/// Provides refresh token related methods.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Creates a new refresh token for a user.
    /// </summary>
    /// <returns>The raw unhashed refresh token.</returns>
    Task<string> CreateAsync(
        Guid userId,
        CancellationToken cancellationToken);
    
    /// <summary>
    /// Rotates a user's refresh token.
    /// </summary>
    /// <param name="oldRefreshToken">The old, used refresh token.</param>
    /// <returns>The raw unhashed refresh token.</returns>
    Task<string> RotateAsync(
        RefreshToken oldRefreshToken,
        CancellationToken cancellationToken);
    
    /// <summary>
    /// Revokes a refresh token.
    /// </summary>
    Task RevokeAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken);
    
    /// <summary>
    /// Revokes all valid refresh tokens belonging to a user.
    /// </summary>
    Task RevokeValidByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);
}