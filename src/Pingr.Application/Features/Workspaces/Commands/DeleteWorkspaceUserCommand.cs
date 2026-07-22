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
/// Deletes a specified user from a workspace.
/// </summary>
/// <param name="UserId">User's identifier.</param>
/// <param name="WorkspaceId">Workspace's identifier.</param>
public sealed record DeleteWorkspaceUserCommand(
    Guid UserId,
    Guid WorkspaceId) : IRequest<Result>;

public sealed class DeleteWorkspaceUserCommandHandler(
    IWorkspaceRepository workspaceRepository,
    IWorkspaceUserRepository workspaceUserRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteWorkspaceUserCommand, Result>
{
    public async Task<Result> Handle(DeleteWorkspaceUserCommand request, CancellationToken cancellationToken)
    {
        // Prevent user from kicking themselves
        if (currentUser.GetUserId() == request.UserId)
            return WorkspaceErrors.ActionForbidden;
        
        // Check if user exists
        if (await workspaceRepository.GetByWorkspaceIdAsync(request.WorkspaceId, cancellationToken) is null)
            return WorkspaceErrors.NotFound;
        
        var requestingWorkspaceUser = await workspaceUserRepository.GetAsync(
            currentUser.GetUserId(),
            request.WorkspaceId,
            cancellationToken);
        
        // Check if user is a member of this workspace
        if (requestingWorkspaceUser is null)
            return WorkspaceErrors.ActionForbidden;
        
        // Check if user has authorization to kick others
        if (requestingWorkspaceUser.Role is not (WorkspaceRole.Admin or WorkspaceRole.Owner))
            return WorkspaceErrors.ActionForbidden;
        
        // Check if target user exists
        var targetWorkspaceUser = await workspaceUserRepository.GetAsync(
            request.UserId,
            request.WorkspaceId,
            cancellationToken);
        
        if (targetWorkspaceUser is null)
            return WorkspaceErrors.UserNotFound;
        
        // Prevent owner being removed
        if (targetWorkspaceUser.Role is WorkspaceRole.Owner)
            return WorkspaceErrors.ActionForbidden;

        // Prevent admins kicking each-other
        if (targetWorkspaceUser.Role is WorkspaceRole.Admin
            && requestingWorkspaceUser.Role != WorkspaceRole.Owner)
            return WorkspaceErrors.ActionForbidden;
        
        // Delete target workspace user
        workspaceUserRepository.Delete(targetWorkspaceUser);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}
    
public sealed class DeleteWorkspaceUserCommandValidator : AbstractValidator<DeleteWorkspaceUserCommand>
{
    public DeleteWorkspaceUserCommandValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("Workspace identifier must not be empty.");
        
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User identifier must not be empty.");
    }
}
