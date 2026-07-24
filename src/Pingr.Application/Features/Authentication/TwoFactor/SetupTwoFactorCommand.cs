using Pingr.Application.Abstractions;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Pingr.Domain.Common;
using Pingr.Domain.Entities;
using MediatR;
using Pingr.Application.Abstractions.Services.Authentication;

namespace Pingr.Application.Features.Authentication.TwoFactor;

/// <summary>
/// Starts setting up 2FA for a user.
/// </summary>
public sealed record SetupTwoFactorCommand() : IRequest<Result<SetupTwoFactorResponse>>;

public sealed class SetupTwoFactorCommandHandler(
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IUserRepository userRepository,
    ITokenGenerator tokenGenerator,
    ITwoFactorChallengeRepository twoFactorChallengeRepository,
    ITwoFactorService twoFactorService) : IRequestHandler<SetupTwoFactorCommand, Result<SetupTwoFactorResponse>>
{
    public async Task<Result<SetupTwoFactorResponse>> Handle(SetupTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(
            currentUser.GetUserId(),
            cancellationToken);
        
        if (user.TwoFactorEnabled)
            return UserErrors.TwoFactorAlreadySetup;

        byte[] secret = twoFactorService.GenerateSecret();
        var twoFactorToken = tokenGenerator.GenerateToken();
        var qrCode = twoFactorService.GenerateQrCode(user.Email, secret);
        
        user.AddTwoFactorSecret(twoFactorService.EncodeSecret(secret));
        
        var existingChallenge = await twoFactorChallengeRepository.GetAsync(user.Id, cancellationToken);
        if (existingChallenge is not null)
        {
            existingChallenge.Update(
                tokenGenerator.HashToken(twoFactorToken),
                TwoFactorChallengePurpose.Confirm2FaSetup);
        }
        else
        {
            await twoFactorChallengeRepository.AddAsync(
                new(
                    user.Id,
                    tokenGenerator.HashToken(twoFactorToken),
                    TwoFactorChallengePurpose.Confirm2FaSetup),
                cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SetupTwoFactorResponse(
            twoFactorToken,
            qrCode);
    }
}

/// <summary>
/// Represents the response returned when starting a 2FA setup.
/// </summary>
public sealed record SetupTwoFactorResponse(
    string TwoFactorToken,
    string QrCode);