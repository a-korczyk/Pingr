namespace Pingr.Application.Abstractions.Services.Authentication;

/// <summary>
/// Provides access to information about the currently authenticated user.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// Retrieves the user's identifier from their JWT token.
    /// </summary>
    /// <returns>The user's identifier.</returns>
    Guid GetUserId();

    /// <summary>
    /// Retrieves the user's email from their JWT token.
    /// </summary>
    /// <returns>The user's email.</returns>
    string GetUserEmail();
}