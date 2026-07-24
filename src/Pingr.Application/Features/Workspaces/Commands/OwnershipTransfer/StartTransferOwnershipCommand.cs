using FluentValidation;
using Pingr.Application.Abstractions;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Pingr.Application.Features.Workspaces.Commands.DeleteWorkspace;
using Pingr.Domain.Common;
using Pingr.Domain.Entities;
using MediatR;
using Pingr.Application.Abstractions.Services.Authentication;

namespace Pingr.Application.Features.Workspaces.Commands.TransferOwnership;

/// <summary>
/// Starts the workspace ownership transfer process.
/// </summary>
/// <param name="TargetUserId">User identifier of the new owner.</param>
public sealed record StartTransferOwnershipCommand(
    Guid WorkspaceId,
    Guid TargetUserId) : IRequest<Result<StartTransferOwnershipResponse>>;

public sealed class StartOwnershipTransferCommandHandler(
    IWorkspaceRepository workspaceRepository,
    IWorkspaceUserRepository workspaceUserRepository,
    ITwoFactorChallengeRepository twoFactorChallengeRepository,
    IUserRepository userRepository,
    ITokenGenerator tokenGenerator,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IRequestHandler<StartTransferOwnershipCommand, Result<StartTransferOwnershipResponse>>
{
    public async Task<Result<StartTransferOwnershipResponse>> Handle(StartTransferOwnershipCommand request, CancellationToken cancellationToken)
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

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        // Check if user has 2FA enabled
        if (user.TwoFactorEnabled is false)
            return UserErrors.TwoFactorRequired;

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
        
        var existingChallenge = await twoFactorChallengeRepository.GetAsync(userId, cancellationToken);
        var twoFactorToken = tokenGenerator.GenerateToken();
        
        if (existingChallenge is not null)
        {
            existingChallenge.Update(
                tokenGenerator.HashToken(twoFactorToken),
                TwoFactorChallengePurpose.TransferOwnership);
        }
        else
        {
            await twoFactorChallengeRepository.AddAsync(
                new(
                    userId,
                    tokenGenerator.HashToken(twoFactorToken),
                    TwoFactorChallengePurpose.TransferOwnership),
                cancellationToken);
        }
        
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new StartTransferOwnershipResponse(
            twoFactorToken);
    }
}

public sealed record StartTransferOwnershipResponse(
    string TwoFactorToken);

public sealed class StartTransferOwnershipCommandValidator : AbstractValidator<StartTransferOwnershipCommand>
{
    public StartTransferOwnershipCommandValidator()
    {
        RuleFor(x => x.TargetUserId)
            .NotEmpty().WithMessage("TargetUserId must not be empty.");
    }
}
