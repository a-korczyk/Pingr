using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Pingr.Domain.Common;
using MediatR;
using Pingr.Application.Abstractions.Services.Authentication;

namespace Pingr.Application.Features.Users.Commands;

/// <summary>
/// Revokes all valid refresh tokens belonging to a user.
/// </summary>
public sealed record LogoutAllSessionsCommand : IRequest<Result>;

public sealed class LogoutAllSessionsCommandHandler(
    ICurrentUser currentUser,
    IRefreshTokenService refreshTokenService) : IRequestHandler<LogoutAllSessionsCommand, Result>
{
    public async Task<Result> Handle(LogoutAllSessionsCommand request, CancellationToken cancellationToken)
    {
        await refreshTokenService.RevokeValidByUserIdAsync(
            currentUser.GetUserId(),
            cancellationToken);

        return Result.Success();
    }
}