using FluentValidation;
using Pingr.Application.Abstractions;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Pingr.Application.Abstractions.Services.Email;
using Pingr.Domain.Common;
using Pingr.Domain.Entities;
using MediatR;
using Pingr.Application.Abstractions.Services.Authentication;

namespace Pingr.Application.Features.Workspaces.Commands.TransferOwnership;

/// <summary>
/// Completes the workspace ownership transfer process.
/// </summary>
/// <param name="TargetUserId">User identifier of the new owner.</param>
public sealed record CompleteTransferOwnershipCommand(
    Guid WorkspaceId,
    Guid TargetUserId,
    string TwoFactorToken,
    string TotpCode) : IRequest<Result>;

public sealed class CompleteTransferOwnershipCommandHandler(
    IWorkspaceRepository workspaceRepository,
    IWorkspaceUserRepository workspaceUserRepository,
    ITwoFactorService twoFactorService,
    ITwoFactorChallengeRepository twoFactorChallengeRepository,
    IUserRepository userRepository,
    ITokenGenerator tokenGenerator,
    IUnitOfWork unitOfWork,
    IEmailSender emailSender,
    ICurrentUser currentUser) : IRequestHandler<CompleteTransferOwnershipCommand, Result>
{
    public async Task<Result> Handle(CompleteTransferOwnershipCommand request, CancellationToken cancellationToken)
    {
        var workspace = await workspaceRepository.GetByWorkspaceIdAsync(
            request.WorkspaceId,
            cancellationToken);

        // Check if workspace exists
        if (workspace is null)
            return WorkspaceErrors.NotFound;
        
        var userId = currentUser.GetUserId();
        
        // Check if workspace belongs to user
        if (workspace.OwnerUserId != userId)
            return WorkspaceErrors.ActionForbidden;

        var targetUser = await userRepository.GetByIdAsync(
            request.TargetUserId,
            cancellationToken);

        var targetWorkspaceUser = await workspaceUserRepository.GetAsync(
            targetUser.Id,
            request.WorkspaceId,
            cancellationToken);

        // Check if target user exists
        if (targetUser is null)
            return UserErrors.NotFound;
        
        // Check if target user is member of workspace
        if (targetWorkspaceUser is null)
            return WorkspaceErrors.UserNotFound;
        
        var challenge = await twoFactorChallengeRepository.GetByTokenHashAsync(
            tokenGenerator.HashToken(request.TwoFactorToken),
            cancellationToken);
        
        // Check if challenge exists and if it has the provided purpose
        var challengeValidation = TwoFactorChallenge.ValidateChallenge(
            challenge,
            TwoFactorChallengePurpose.DeleteWorkspace);

        if (challengeValidation.IsFailure)
            return challengeValidation.Error;

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        
        // Check totp code
        var isTotpCodeValid = twoFactorService.VerifyTotpCode(
            request.TotpCode,
            user.TwoFactorSecret);

        if (!isTotpCodeValid)
            return TwoFactorErrors.InvalidTotpCode;

        twoFactorChallengeRepository.Delete(challenge);
        
        workspace.TransferOwnership(targetUser.Id);
        
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Try sending notifaction email to target user
        try
        {
            var emailTemplate = WorkspaceEmailTemplates.OwnerOfNewWorkspace(workspace.Name);
            
            await emailSender.SendAsync(
                new EmailMessageDetails(
                    targetUser.Email,
                    null,
                    emailTemplate.Subject,
                    emailTemplate.Body),
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.ToString());
        }

        return Result.Success();
    }
}

public sealed class CompleteTransferOwnershipCommandValidator : AbstractValidator<CompleteTransferOwnershipCommand>
{
    public CompleteTransferOwnershipCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("WorkspaceId must not be empty.");
        
        RuleFor(x => x.TargetUserId)
            .NotEmpty().WithMessage("TargetUserId must not be empty.");
        
        RuleFor(x => x.TwoFactorToken)
            .NotEmpty().WithMessage("TwoFactorToken must not be empty.")
            .MaximumLength(255).WithMessage("TwoFactorToken must not exceed 255 characters.");

        RuleFor(x => x.TotpCode)
            .NotEmpty().WithMessage("TotpCode must not be empty.")
            .Length(6).WithMessage("TotpCode must be 6 digits.")
            .Matches(@"^\d{6}$").WithMessage("TotpCode must contain only digits.");
    }
}