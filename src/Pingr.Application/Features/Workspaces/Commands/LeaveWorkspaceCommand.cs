using FluentValidation;
using Pingr.Application.Abstractions;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Pingr.Domain.Common;
using Pingr.Domain.Entities;
using MediatR;
using Pingr.Application.Abstractions.Services.Authentication;

namespace Pingr.Application.Features.Workspaces.Commands;

/// <summary>
/// Allows a user to leave a workspace.
/// </summary>
/// <param name="WorkspaceId">Workspace's identifier.</param>
public sealed record LeaveWorkspaceCommand(
    Guid WorkspaceId) : IRequest<Result>;

public sealed class LeaveWorkspaceCommandHandler(
    IWorkspaceRepository workspaceRepository,
    IWorkspaceUserRepository workspaceUserRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<LeaveWorkspaceCommand, Result>
{
    public async Task<Result> Handle(LeaveWorkspaceCommand request, CancellationToken cancellationToken)
    {
        // Check if workspace exists
        if (await workspaceRepository.GetByWorkspaceIdAsync(request.WorkspaceId, cancellationToken) is null)
            return WorkspaceErrors.NotFound;
        
        var workspaceUser = await workspaceUserRepository.GetAsync(
            currentUser.GetUserId(),
            request.WorkspaceId,
            cancellationToken);
        
        // Check if user is member of workspace
        if (workspaceUser is null)
            return WorkspaceErrors.ActionForbidden;
        
        // Prevent owner from leaving
        if (workspaceUser.Role is WorkspaceRole.Owner)
            return WorkspaceErrors.ActionForbidden; 

        // Delete workspace user
        workspaceUserRepository.Delete(workspaceUser);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public class LeaveWorkspaceCommandValidator : AbstractValidator<LeaveWorkspaceCommand>
{
    public LeaveWorkspaceCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace identifier must not be empty.");
    }
}