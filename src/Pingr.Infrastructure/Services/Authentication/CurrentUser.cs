using System.Security.Claims;
using Pingr.Application.Abstractions.Services;
using Microsoft.AspNetCore.Http;
using Pingr.Application.Abstractions.Services.Authentication;

namespace Pingr.Infrastructure.Services.Authentication;

/// <summary>
/// Retrieves information about the currently authenticated user
/// from the current HTTP request.
/// </summary>
public sealed class CurrentUser(
    IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid GetUserId()
    {
        string? userId = httpContextAccessor.HttpContext?
            .User
            .FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (!Guid.TryParse(userId, out Guid parsedUserId))
            throw new UnauthorizedAccessException();

        return parsedUserId;
    }
    
    public string GetUserEmail()
    {
        string? userEmail = httpContextAccessor.HttpContext?
            .User
            .FindFirstValue(ClaimTypes.Email);
        
        return userEmail ?? throw new UnauthorizedAccessException();
    }
}