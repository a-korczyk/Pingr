using System.Security.Cryptography;
using System.Text;
using Pingr.Application.Abstractions;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Pingr.Application.Abstractions.Services.Email;
using Pingr.Domain.Common;
using MediatR;
using Pingr.Application.Abstractions.Services.Authentication;

namespace Pingr.Application.Features.Authentication.Commands;

/// <summary>
/// Verifies the user's email.
/// </summary>
/// <param name="UserId">The user's identifier.</param>
/// <param name="Token">The verification token.</param>
public sealed record VerifyCommand(
    Guid UserId,
    string Token) : IRequest<Result>;
    
/// <summary>
/// Handles <see cref="VerifyCommand"/> requests.
/// </summary>
public sealed class VerifyCommandHandler(
    ITokenGenerator tokenGenerator,
    IEmailVerificationRequestRepository emailVerificationRequestRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IEmailSender emailSender) : IRequestHandler<VerifyCommand, Result>
{
    public async Task<Result> Handle(VerifyCommand request, CancellationToken cancellationToken)
    {
        var emailVerificationRequest = await emailVerificationRequestRepository.GetAsync(
            request.UserId,
            cancellationToken);

        // Check if email verification request and command request are valid 
        if (emailVerificationRequest is null)
            return EmailVerificationRequestErrors.NotFound;

        if (emailVerificationRequest.ExpiresAt < DateTimeOffset.UtcNow)
        {
            await emailVerificationRequestRepository.DeleteAsync(
                emailVerificationRequest,
                cancellationToken);
            
            return EmailVerificationRequestErrors.Expired;
        }

        // Check if tokens match
        var hashedToken = tokenGenerator.HashToken(request.Token);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(hashedToken),
                Encoding.UTF8.GetBytes(emailVerificationRequest.TokenHash)))
            return EmailVerificationRequestErrors.Invalid;
        
        // Updates user's EmailConfirmed property.
        var user = await userRepository.GetByIdAsync(
            emailVerificationRequest.UserId,
            cancellationToken);
        
        user!.ConfirmEmail();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        // Delete after getting user, because the verification request's
        // userId field is needed.
        await emailVerificationRequestRepository.DeleteAsync(
            emailVerificationRequest,
            cancellationToken);
        
        try
        {
            await emailSender.SendAsync(
                new(
                    user.Email,
                    null,
                    AuthEmailTemplates.EmailVerified.Subject,
                    AuthEmailTemplates.EmailVerified.Body),
                cancellationToken);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.ToString());
        }
        
        return Result.Success();
    }
}