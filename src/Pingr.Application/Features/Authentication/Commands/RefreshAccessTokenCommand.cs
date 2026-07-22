using FluentValidation;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Pingr.Domain.Common;
using MediatR;
using Pingr.Application.Abstractions.Services.Authentication;

namespace Pingr.Application.Features.Authentication.Commands;

/// <summary>
/// Generates a new access token for the user and rotates their refresh token.
/// </summary>
/// <param name="RefreshToken"></param>
public sealed record RefreshAccessTokenCommand(
    string RefreshToken) : IRequest<Result<RefreshAccessTokenResponse>>;

public sealed class RefreshAccessTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    ICurrentUser currentUser,
    IUserRepository userRepository,
    IRefreshTokenService refreshTokenService,
    ITokenGenerator tokenGenerator,
    IJwtProvider jwtProvider) : IRequestHandler<RefreshAccessTokenCommand, Result<RefreshAccessTokenResponse>>
{
    public async Task<Result<RefreshAccessTokenResponse>> Handle(RefreshAccessTokenCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();
        
        var oldRefreshTokenEntity = await refreshTokenRepository.GetAsync(
            userId, 
            tokenGenerator.HashToken(request.RefreshToken),
            cancellationToken);

        if (oldRefreshTokenEntity is null)
            return RefreshTokenErrors.NotFound;

        if (oldRefreshTokenEntity.ExpiresAt < DateTimeOffset.UtcNow)
            return RefreshTokenErrors.Expired;

        if (oldRefreshTokenEntity.RevokedAt is not null
            && oldRefreshTokenEntity.RevokedAt < DateTimeOffset.UtcNow)
            return RefreshTokenErrors.Revoked;

        var newRefreshToken = await refreshTokenService.RotateAsync(
            oldRefreshTokenEntity,
            cancellationToken);

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        var newAccessToken = jwtProvider.CreateToken(user);
        
        return new RefreshAccessTokenResponse(
            newAccessToken,
            newRefreshToken);
    }
}

public sealed record RefreshAccessTokenResponse(
    string NewAccessToken,
    string NewRefreshToken);

public sealed class RefreshAccessTokenCommandValidator : AbstractValidator<RefreshAccessTokenCommand>
{
    public RefreshAccessTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("RefreshTokenHash must not be empty.");
    }
}