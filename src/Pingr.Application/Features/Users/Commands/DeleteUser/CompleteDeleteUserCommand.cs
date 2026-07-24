using FluentValidation;
using Pingr.Application.Abstractions;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Pingr.Domain.Common;
using Pingr.Domain.Entities;
using MediatR;
using Pingr.Application.Abstractions.Services.Authentication;

namespace Pingr.Application.Features.Users.Commands.DeleteUser;

/// <summary>
/// Finishes the process of deleting a user's account.
/// </summary>
public sealed record CompleteDeleteUserCommand(
    Guid UserId,
    string TwoFactorToken,
    string TotpCode) : IRequest<Result>;

public sealed class CompleteDeleteUserCommandHandler(
    ITwoFactorChallengeRepository twoFactorChallengeRepository,
    ITwoFactorService twoFactorService,
    IRefreshTokenService refreshTokenService,
    ITokenGenerator tokenGenerator,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<CompleteDeleteUserCommand, Result>
{
    public async Task<Result> Handle(CompleteDeleteUserCommand request, CancellationToken cancellationToken)
    {
        var challenge = await twoFactorChallengeRepository.GetByTokenHashAsync(
            tokenGenerator.HashToken(request.TwoFactorToken),
            cancellationToken);
        
        var challegeValidation = TwoFactorChallenge.ValidateChallenge(
            challenge,
            TwoFactorChallengePurpose.DeleteAccount);

        // Check challenge
        if (challegeValidation.IsFailure)
            return challegeValidation.Error;

        var user = await userRepository.GetByIdAsync(
            challenge.UserId,
            cancellationToken);

        var isTotpCodeValid = twoFactorService.VerifyTotpCode(
            request.TotpCode,
            user.TwoFactorSecret);

        // Check totp code
        if (!isTotpCodeValid)
            return TwoFactorErrors.InvalidTotpCode;

        // Delete challenge
        twoFactorChallengeRepository.Delete(challenge);

        // Revoke all refresh tokens
        await refreshTokenService.RevokeValidByUserIdAsync(
            user.Id,
            cancellationToken);
        
        // Delete user
        userRepository.Delete(user);
        
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class CompleteDeleteUserCommandValidator : AbstractValidator<CompleteDeleteUserCommand>
{
    public CompleteDeleteUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId must not be empty.");
        
        RuleFor(x => x.TwoFactorToken)
            .NotEmpty().WithMessage("TwoFactorToken must not be empty.")
            .MaximumLength(255).WithMessage("TwoFactorToken must not exceed 255 characters.");

        RuleFor(x => x.TotpCode)
            .NotEmpty().WithMessage("TotpCode must not be empty.")
            .Length(6).WithMessage("TotpCode must be 6 digits.")
            .Matches(@"^\d{6}$").WithMessage("TotpCode must contain only digits.");
    }
}