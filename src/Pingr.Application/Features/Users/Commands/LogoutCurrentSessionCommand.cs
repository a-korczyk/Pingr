using FluentValidation;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Pingr.Domain.Common;
using MediatR;
using Pingr.Application.Abstractions.Services.Authentication;

namespace Pingr.Application.Features.Users.Commands;

/// <summary>
/// Revokes the user's current session.
/// </summary>
public sealed record LogoutCurrentSessionCommand(
    string RefreshToken) : IRequest<Result>;

public sealed class LogoutCurrentSessionCommandHandler(
    ICurrentUser currentUser,
    ITokenGenerator tokenGenerator,
    IRefreshTokenService refreshTokenService,
    IRefreshTokenRepository refreshTokenRepository) : IRequestHandler<LogoutCurrentSessionCommand, Result>
{
    public async Task<Result> Handle(LogoutCurrentSessionCommand request, CancellationToken cancellationToken)
    {
        var refreshTokenEntity = await refreshTokenRepository.GetAsync(
            currentUser.GetUserId(), 
            tokenGenerator.HashToken(request.RefreshToken),
            cancellationToken);

        if (refreshTokenEntity is null)
            return RefreshTokenErrors.NotFound;

        if (refreshTokenEntity.ExpiresAt < DateTimeOffset.UtcNow)
            return RefreshTokenErrors.Expired;

        if (refreshTokenEntity.RevokedAt is not null
            && refreshTokenEntity.RevokedAt < DateTimeOffset.UtcNow)
            return RefreshTokenErrors.Revoked;

        await refreshTokenService.RevokeAsync(
            refreshTokenEntity,
            cancellationToken);

        return Result.Success();
    }
}

public sealed class LogoutCurrentSessionCommandValidator : AbstractValidator<LogoutCurrentSessionCommand>
{
    public LogoutCurrentSessionCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("RefreshToken must not be empty.");
    }
}