using FluentValidation;
using Pingr.Application.Abstractions;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Pingr.Domain.Common;
using Pingr.Domain.Entities;
using MediatR;
using Pingr.Application.Abstractions.Services.Authentication;

namespace Pingr.Application.Features.Workspaces.Commands.DeleteWorkspace;

/// <summary>
/// Starts the workspace deletion process.
/// </summary>
/// <param name="WorkspaceId"></param>
public sealed record StartDeleteWorkspaceCommand(
    Guid WorkspaceId) : IRequest<Result<StartDeleteWorkspaceResponse>>;
    
public sealed class StartDeleteWorkspaceCommandHandler(
    IWorkspaceRepository workspaceRepository,
    ITwoFactorChallengeRepository twoFactorChallengeRepository,
    IUserRepository userRepository,
    ITokenGenerator tokenGenerator,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IRequestHandler<StartDeleteWorkspaceCommand, Result<StartDeleteWorkspaceResponse>>
{
    public async Task<Result<StartDeleteWorkspaceResponse>> Handle(StartDeleteWorkspaceCommand request, CancellationToken cancellationToken)
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
        
        var existingChallenge = await twoFactorChallengeRepository.GetAsync(userId, cancellationToken);
        var twoFactorToken = tokenGenerator.GenerateToken();
        
        if (existingChallenge is not null)
        {
            existingChallenge.Update(
                tokenGenerator.HashToken(twoFactorToken),
                TwoFactorChallengePurpose.DeleteWorkspace);
        }
        else
        {
            await twoFactorChallengeRepository.AddAsync(
                new(
                    userId,
                    tokenGenerator.HashToken(twoFactorToken),
                    TwoFactorChallengePurpose.DeleteWorkspace),
                cancellationToken);
        }
        
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new StartDeleteWorkspaceResponse(
            twoFactorToken);
    }
}

public sealed record StartDeleteWorkspaceResponse(
    string twoFactorToken);

public sealed class StartDeleteWorkspaceCommandValidator : AbstractValidator<StartDeleteWorkspaceCommand>
{
    public StartDeleteWorkspaceCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("WorkspaceId must not be empty.");
    }
}