using FluentValidation;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Pingr.Application.Abstractions.Services.Email;
using Pingr.Domain.Common;
using Pingr.Domain.Entities;
using MediatR;
using Pingr.Application.Abstractions.Services.Authentication;

namespace Pingr.Application.Features.Authentication.Commands;

/// <summary>
/// Creates a new user with the provided <c>Email</c> and <c>Password</c> and sends a verification email.
/// </summary>
public sealed record RegisterCommand(
    string Email,
    string Password) : IRequest<Result>;

/// <summary>
/// Handles new user creation and sending an email with a verification link.
/// </summary>
public sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator,
    IEmailVerificationRequestRepository emailVerificationRequestRepository,
    IEmailSender emailSender)
    : IRequestHandler<RegisterCommand, Result>
{
    public async Task<Result> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.GetByEmailAsync(request.Email, cancellationToken) != null)
            return UserErrors.EmailAlreadyExists;
        
        string hashedPassword = passwordHasher.HashPassword(request.Password, cancellationToken);
        
        User user = new User(
            request.Email,
            hashedPassword);
        
        await userRepository.AddAsync(user, cancellationToken);
        
        // Add email verification request
        var token = tokenGenerator.GenerateToken();
        var hashedToken = tokenGenerator.HashToken(token);
        
        await emailVerificationRequestRepository.AddAsync(
            new(
                user.Id,
                user,
                hashedToken),
            cancellationToken);
        
        // Send email verification email.
        var verificationUrl = $"http://localhost:8080/api/v1/auth/verify?userId={user.Id}&token={token}";
        var emailTemplate = AuthEmailTemplates.VerifyEmail(verificationUrl);
        
        await emailSender.SendAsync(
            new(
                user.Email,
                null,
                emailTemplate.Subject,
                emailTemplate.Body),
            cancellationToken);

        return Result.Success();
    }
}

/// <summary>
/// Validates data when registering a new user.
/// </summary>
public sealed class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        // Email rules
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("Email must not be empty")
            .MaximumLength(255).WithMessage("Email length must not exceed 255 characters")
            .EmailAddress().WithMessage("Invalid email address format");
        
        // Password rules
        RuleFor(command => command.Password)
            .NotEmpty().WithMessage("Password must not be empty")
            .MinimumLength(8).WithMessage("Password length must not be less than 8 characters")
            .MaximumLength(255).WithMessage("Password length must not exceed 255 characters");
    }
}
