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
/// Changes a user's role in a workspace.
/// </summary>
/// <param name="UserId">User's identifier.</param>
/// <param name="WorkspaceId">Workspace's identifier.</param>
/// <param name="NewRole">The new role.</param>
public sealed record ChangeWorkspaceUserRoleCommand(
    Guid UserId,
    Guid WorkspaceId,
    WorkspaceRole NewRole) : IRequest<Result>;
    
public sealed class ChangeWorkspaceUserRoleCommandHandler(
    IWorkspaceRepository workspaceRepository,
    IWorkspaceUserRepository workspaceUserRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<ChangeWorkspaceUserRoleCommand, Result>
{
    public async Task<Result> Handle(ChangeWorkspaceUserRoleCommand request, CancellationToken cancellationToken)
    {
        // Prevent adding another owner
        if (request.NewRole is WorkspaceRole.Owner)
            return WorkspaceErrors.ActionForbidden;

        var workspace = await workspaceRepository.GetByWorkspaceIdAsync(request.WorkspaceId, cancellationToken);
        
        // Check if workspace exists
        if (workspace is null)
            return WorkspaceErrors.NotFound;
        
        var isRequestingUserAuthorized = await workspaceUserRepository.IsInRoleAsync(
            currentUser.GetUserId(),
            request.WorkspaceId,
            [WorkspaceRole.Owner],
            cancellationToken);
        
        // Check if requesting workspace user is authorized to change roles
        if (isRequestingUserAuthorized is false)
            return WorkspaceErrors.ActionForbidden;
        
        var targetWorkspaceUser = await workspaceUserRepository.GetAsync(
            request.UserId,
            request.WorkspaceId,
            cancellationToken);
        
        // Check if target workspace user exists
        if (targetWorkspaceUser is null)
            return WorkspaceErrors.UserNotFound;

        // Prevent changing the owners role
        if (targetWorkspaceUser.UserId == workspace.OwnerUserId)
            return WorkspaceErrors.ActionForbidden;
        
        // Update role
        targetWorkspaceUser.UpdateRole(request.NewRole);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class ChangeWorkspaceUserRoleCommandValidator : AbstractValidator<ChangeWorkspaceUserRoleCommand>
{
    public ChangeWorkspaceUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User identifier must not be empty.");
        
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace identifier must not be empty.");

        RuleFor(x => x.NewRole)
            .IsInEnum().WithMessage("NewRole must only contain WorkspaceRole enum values.");
    }
}