using FluentValidation;
using MediatR;
using Pingr.Application.Abstractions;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services.Authentication;
using Pingr.Domain.Common;

namespace Pingr.Application.Features.Monitors.Commands;

public sealed record DisableMonitorCommand(
    Guid WorkspaceId,
    Guid MonitorId) : IRequest<Result>;
    
public sealed class DisableMonitorCommandHandler(
    ICurrentUser currentUser,
    IWorkspaceUserRepository workspaceUserRepository,
    IMonitorRepository monitorRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<DisableMonitorCommand, Result>
{
    public async Task<Result> Handle(DisableMonitorCommand request, CancellationToken cancellationToken)
    {
        Guid userId = currentUser.GetUserId();
        if (!await workspaceUserRepository.IsMemberAsync(
                userId,
                request.WorkspaceId,
                cancellationToken))
            return MonitorErrors.Forbidden;
        
        var monitor = await monitorRepository.GetByIdAsync(
            request.MonitorId,
            cancellationToken);

        if (monitor == null)
            return MonitorErrors.NotFound;
        
        if (monitor.WorkspaceId != request.WorkspaceId)
            return MonitorErrors.Forbidden;

        monitor.Disable();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
    
public sealed class DisableMonitorValidator : AbstractValidator<DisableMonitorCommand>
{
    public DisableMonitorValidator()
    {
        RuleFor(query => query.WorkspaceId)
            .NotEmpty().WithMessage("Workspace Id must not be empty.");
        
        RuleFor(query => query.MonitorId)
            .NotEmpty().WithMessage("Monitor Id must not be empty.");
    }
}
