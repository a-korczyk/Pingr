using Pingr.Application.Abstractions;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Pingr.Domain.Entities;
using Microsoft.Extensions.Options;
using Pingr.Application.Abstractions.Services.Authentication;

namespace Pingr.Infrastructure.Services.Authentication;

/// <inheritdoc/>
public sealed class RefreshTokenService(
    IRefreshTokenRepository refreshTokenRepository,
    IOptions<RefreshTokenOptions> refreshTokenOptions,
    ITokenGenerator tokenGenerator,
    IUnitOfWork unitOfWork) : IRefreshTokenService
{
    private readonly RefreshTokenOptions _refreshTokenOptions = refreshTokenOptions.Value;

    public async Task<string> CreateAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var newRawRefreshToken = tokenGenerator.GenerateToken();

        var newRefreshToken = new RefreshToken(
            userId,
            tokenGenerator.HashToken(newRawRefreshToken),
            DateTimeOffset.UtcNow.AddDays(
                _refreshTokenOptions.ExpirationInDays));
        
        await refreshTokenRepository.AddAsync(
            newRefreshToken,
            cancellationToken);
        
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return newRawRefreshToken;
    }
    
    public async Task<string> RotateAsync(
        RefreshToken oldRefreshToken,
        CancellationToken cancellationToken)
    {
        var newRawRefreshToken = tokenGenerator.GenerateToken();

        var newRefreshToken = new RefreshToken(
            oldRefreshToken.UserId,
            tokenGenerator.HashToken(newRawRefreshToken),
            DateTimeOffset.UtcNow.AddDays(
                _refreshTokenOptions.ExpirationInDays));
            
        oldRefreshToken.Revoke();
        
        await refreshTokenRepository.AddAsync(
            newRefreshToken,
            cancellationToken);
        
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return newRawRefreshToken;
    }

    public async Task RevokeAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken)
    {
        refreshToken.Revoke();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
    
    public async Task RevokeValidByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var userRefreshTokens = await refreshTokenRepository.GetValidByUserIdAsync(
            userId,
            cancellationToken);

        foreach (var refreshToken in userRefreshTokens)
        {
            refreshToken.Revoke();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}