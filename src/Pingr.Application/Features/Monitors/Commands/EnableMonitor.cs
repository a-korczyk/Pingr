using FluentValidation;
using MediatR;
using Pingr.Application.Abstractions;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Pingr.Application.Abstractions.Services.Authentication;
using Pingr.Domain.Common;

namespace Pingr.Application.Features.Monitors.Commands;

public sealed record EnableMonitorCommand(
    Guid WorkspaceId,
    Guid MonitorId) : IRequest<Result>;
    
public sealed class EnableMonitorCommandHandler(
    ICurrentUser currentUser,
    IWorkspaceUserRepository workspaceUserRepository,
    IMonitorRepository monitorRepository,
    IMonitorQueue monitorQueue,
    IUnitOfWork unitOfWork) : IRequestHandler<EnableMonitorCommand, Result>
{
    public async Task<Result> Handle(EnableMonitorCommand request, CancellationToken cancellationToken)
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
        
        monitor.Enable();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        monitorQueue.Add(monitor);

        return Result.Success();
    }
}
    
public sealed class EnableMonitorValidator : AbstractValidator<EnableMonitorCommand>
{
    public EnableMonitorValidator()
    {
        RuleFor(query => query.WorkspaceId)
            .NotEmpty().WithMessage("Workspace Id must not be empty.");
        
        RuleFor(query => query.MonitorId)
            .NotEmpty().WithMessage("Monitor Id must not be empty.");
    }
}
