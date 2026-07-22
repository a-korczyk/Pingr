using FluentValidation;
using Pingr.Application.Abstractions;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Pingr.Domain.Common;
using Pingr.Domain.Entities;
using MediatR;
using Pingr.Application.Abstractions.Services.Authentication;

namespace Pingr.Application.Features.Authentication.TwoFactor;

/// <summary>
/// Confirms a request to set up 2FA for a user.
/// </summary>
/// <param name="TwoFactorToken">The challenge's token.</param>
/// <param name="TotpCode">The Totp code.</param>
public sealed record ConfirmTwoFactorCommand(
    string TwoFactorToken,
    string TotpCode) : IRequest<Result<ConfirmTwoFactorResponse>>;

public sealed class ConfirmTwoFactorCommandHandler(
    IUserRepository userRepository,
    ICurrentUser currentUser,
    ITokenGenerator tokenGenerator,
    ITwoFactorChallengeRepository twoFactorChallengeRepository,
    ITwoFactorService twoFactorService,
    IUnitOfWork unitOfWork) : IRequestHandler<ConfirmTwoFactorCommand, Result<ConfirmTwoFactorResponse>>
{
    public async Task<Result<ConfirmTwoFactorResponse>> Handle(ConfirmTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(
            currentUser.GetUserId(),
            cancellationToken);
        
        if (user.TwoFactorEnabled)
            return UserErrors.TwoFactorAlreadySetup;

        if (user.TwoFactorSecret is null)
            return UserErrors.TwoFactorSetupNotRequested;
        
        var twoFactorChallenge = await twoFactorChallengeRepository.GetAsync(
            user.Id,
            cancellationToken);

        // Check challenge
        var challengeValidation = TwoFactorChallenge.ValidateChallenge(
            twoFactorChallenge,
            TwoFactorChallengePurpose.Confirm2FaSetup,
            expectedTokenHash: tokenGenerator.HashToken(request.TwoFactorToken));

        if (challengeValidation.IsFailure)
            return challengeValidation.Error;
        
        
        var isTotpCodeValid = twoFactorService.VerifyTotpCode(
            request.TotpCode,
            user.TwoFactorSecret);

        if (!isTotpCodeValid)
            return TwoFactorErrors.InvalidTotpCode;
        
        // Generate and save recovery codes
        var recoveryCodes = twoFactorService.GenerateRecoveryCodes();
        user.AddTwoFactorRecoveryCodes(twoFactorService.HashRecoveryCodes(recoveryCodes));
        
        user.EnableTwoFactor();
        twoFactorChallengeRepository.Delete(twoFactorChallenge);
        
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return new ConfirmTwoFactorResponse(recoveryCodes);
    }
}

/// <summary>
/// Represents a successful 2FA setup for an account.
/// </summary>
public sealed record ConfirmTwoFactorResponse(
    IList<string> RecoveryCodes);

public sealed class ConfirmCommandValidator : AbstractValidator<ConfirmTwoFactorCommand>
{
    public ConfirmCommandValidator()
    {
        RuleFor(x => x.TwoFactorToken)
            .NotEmpty().WithMessage("TwoFactorToken must not be empty.")
            .MaximumLength(255).WithMessage("TwoFactorToken must not exceed 255 characters.");

        RuleFor(x => x.TotpCode)
            .NotEmpty().WithMessage("TotpCode must not be empty.")
            .Length(6).WithMessage("TotpCode must be 6 digits.")
            .Matches(@"^\d{6}$").WithMessage("TotpCode must contain only digits.");
    }
}