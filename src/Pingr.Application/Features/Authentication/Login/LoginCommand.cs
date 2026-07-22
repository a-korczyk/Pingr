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
/// Authenticates a user by using their email address and password and either
/// issues a JWT or starts the 2FA flow.
/// </summary>
public sealed record LoginCommand(
    string Email,
    string Password) : IRequest<Result<LoginResponse>>;

/// <summary>
/// Handles user authentication and decides if 2FA is required.
/// </summary>
public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtProvider jwtProvider,
    ITwoFactorChallengeRepository twoFactorChallengeRepository,
    ITokenGenerator tokenGenerator,
    IRefreshTokenService refreshTokenService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        User? user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
            return UserErrors.InvalidCredentials;
        if (user.EmailConfirmed is false)
            return UserErrors.UnverifiedEmail;
        
        bool isPasswordValid = passwordHasher.VerifyPassword(
            request.Password,
            user.PasswordHash,
            cancellationToken);
        if (!isPasswordValid)
            return UserErrors.InvalidCredentials;

        if (!user.TwoFactorEnabled)
        {
            string accessToken = jwtProvider.CreateToken(user);

            string refreshToken = await refreshTokenService.CreateAsync(
                user.Id,
                cancellationToken);
            
            return new LoginResponse(
                UserId: user.Id,
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                TwoFactorToken: null,
                RequiresTwoFactor: false);
        }

        var existingChallenge = await twoFactorChallengeRepository.GetAsync(user.Id, cancellationToken);
        var twoFactorToken = tokenGenerator.GenerateToken();
        
        if (existingChallenge is not null)
        {
            existingChallenge.Update(
                tokenGenerator.HashToken(twoFactorToken),
                TwoFactorChallengePurpose.Login);
        }
        else
        {
            await twoFactorChallengeRepository.AddAsync(
                new(
                    user.Id,
                    tokenGenerator.HashToken(twoFactorToken),
                    TwoFactorChallengePurpose.Login),
                cancellationToken);
        }
        
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResponse(
            UserId: user.Id,
            AccessToken: null,
            RefreshToken: null,
            TwoFactorToken: twoFactorToken,
            RequiresTwoFactor: true);
    }
}

/// <summary>
/// Represents the result of a login attempt, containing either a
/// JWT or a 2FA challenge token.
/// </summary>
public sealed record LoginResponse(
    Guid UserId,
    string? AccessToken,
    string? RefreshToken,
    string? TwoFactorToken,
    bool RequiresTwoFactor);

/// <summary>
/// Validates data provided when logging in.
/// </summary>
public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        // Email rules
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("Email must not be empty")
            .EmailAddress().WithMessage("Invalid email address format");
        
        // Password rules
        RuleFor(command => command.Password)
            .NotEmpty().WithMessage("Password must not be empty");
    }
}