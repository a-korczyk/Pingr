using FluentValidation;
using Pingr.Application.Abstractions;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Pingr.Domain.Common;
using Pingr.Domain.Entities;
using MediatR;
using Pingr.Application.Abstractions.Services.Authentication;

namespace Pingr.Application.Features.Authentication.Login;

/// <summary>
/// Completes the login flow by verifying the user's 2FA code.
/// </summary>
public sealed record CompleteTwoFactorLoginCommand(
    string TwoFactorToken,
    string TotpCode) : IRequest<Result<CompleteTwoFactorLoginResponse>>;

public sealed class CompleteTwoFactorLoginCommandHandler(
    IUserRepository userRepository,
    ITwoFactorChallengeRepository twoFactorChallengeRepository,
    ITwoFactorService twoFactorService,
    ITokenGenerator tokenGenerator,
    IUnitOfWork unitOfWork,
    IRefreshTokenService refreshTokenService,
    IJwtProvider jwtProvider) : IRequestHandler<CompleteTwoFactorLoginCommand, Result<CompleteTwoFactorLoginResponse>>
{
    public async Task<Result<CompleteTwoFactorLoginResponse>> Handle(CompleteTwoFactorLoginCommand request, CancellationToken cancellationToken)
    {
        var challenge = await twoFactorChallengeRepository.GetByTokenHashAsync(
            tokenGenerator.HashToken(request.TwoFactorToken),
            cancellationToken);
        
        // Check challenge
        var challegeValidation = TwoFactorChallenge.ValidateChallenge(
            challenge,
            TwoFactorChallengePurpose.Login);

        if (challegeValidation.IsFailure)
            return challegeValidation.Error;

        var user = await userRepository.GetByIdAsync(
            challenge.UserId,
            cancellationToken);
        
        // Check user
        if (user is null)
            return UserErrors.NotFound;

        var isTotpCodeValid = twoFactorService.VerifyTotpCode(
            request.TotpCode,
            user.TwoFactorSecret);

        if (!isTotpCodeValid)
            return TwoFactorErrors.InvalidTotpCode;

        twoFactorChallengeRepository.Delete(challenge);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = jwtProvider.CreateToken(user);
        
        string refreshToken = await refreshTokenService.CreateAsync(
            user.Id,
            cancellationToken);
        
        return new CompleteTwoFactorLoginResponse(
            user.Id,
            accessToken,
            refreshToken);
    }
}

/// <summary>
/// Represents a successful login with 2FA. 
/// </summary>
public sealed record CompleteTwoFactorLoginResponse(
    Guid UserId,
    string AccessToken,
    string RefreshToken);

public sealed class CompleteTwoFactorLoginCommandValidator : AbstractValidator<CompleteTwoFactorLoginCommand>
{
    public CompleteTwoFactorLoginCommandValidator()
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
